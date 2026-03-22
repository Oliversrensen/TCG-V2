using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Core.Services;
using TCG.GameLogic;
using TCG.Server.Data;

namespace TCG.Server.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IMatchmakingService _matchmaking;
    private readonly IGameEngine _gameEngine;
    private readonly IMatchStateStore _matchState;
    private readonly MatchConnectionStore _connections;
    private readonly IServiceScopeFactory _scopeFactory;

    public GameHub(
        IMatchmakingService matchmaking,
        IGameEngine gameEngine,
        IMatchStateStore matchState,
        MatchConnectionStore connections,
        IServiceScopeFactory scopeFactory)
    {
        _matchmaking = matchmaking;
        _gameEngine = gameEngine;
        _matchState = matchState;
        _connections = connections;
        _scopeFactory = scopeFactory;
    }

    private string? UserId => Context.User?.FindFirst("sub")?.Value
        ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task JoinQueue(Guid deckId)
    {
        if (string.IsNullOrEmpty(UserId))
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }
        await _matchmaking.JoinQueueAsync(UserId, deckId, Context.ConnectionId);
    }

    public async Task LeaveQueue()
    {
        await _matchmaking.LeaveQueueAsync(Context.ConnectionId);
    }

    public async Task PlayCard(Guid matchId, Guid cardId, Guid? targetId)
    {
        if (string.IsNullOrEmpty(UserId)) return;
        var state = _matchState.Get(matchId);
        if (state is null) return;
        try
        {
            var next = _gameEngine.PlayCard(state, UserId, cardId, targetId);
            _matchState.Set(matchId, next);
            await BroadcastStateAsync(matchId, next);
        }
        catch (InvalidOperationException)
        {
            await Clients.Caller.SendAsync("Error", "Invalid move");
        }
    }

    public async Task Attack(Guid matchId, string attackerInstanceId, string targetInstanceId)
    {
        if (string.IsNullOrEmpty(UserId)) return;
        var state = _matchState.Get(matchId);
        if (state is null) return;
        try
        {
            var next = _gameEngine.Attack(state, UserId, attackerInstanceId, targetInstanceId);
            _matchState.Set(matchId, next);
            var winner = _gameEngine.GetWinner(next);
            if (winner is not null)
            {
                _matchState.Remove(matchId);
                _connections.Remove(matchId);
                await BroadcastStateAsync(matchId, next);
                await BroadcastGameOverAsync(matchId, winner);
            }
            else
            {
                await BroadcastStateAsync(matchId, next);
            }
        }
        catch (InvalidOperationException)
        {
            await Clients.Caller.SendAsync("Error", "Invalid attack");
        }
    }

    public async Task RejoinMatch(Guid matchId)
    {
        if (string.IsNullOrEmpty(UserId)) return;
        var ok = _connections.Reconnect(matchId, UserId, Context.ConnectionId);
        if (!ok)
        {
            await Clients.Caller.SendAsync("Error", "Match not found or you are not in this match");
            return;
        }
        var state = _matchState.Get(matchId);
        if (state is not null)
            await Clients.Caller.SendAsync("StateUpdate", state);
    }

    public async Task EndTurn(Guid matchId)
    {
        if (string.IsNullOrEmpty(UserId)) return;
        var state = _matchState.Get(matchId);
        if (state is null) return;
        try
        {
            var next = _gameEngine.EndTurn(state, UserId);
            _matchState.Set(matchId, next);
            await BroadcastStateAsync(matchId, next);
        }
        catch (InvalidOperationException)
        {
            await Clients.Caller.SendAsync("Error", "Invalid move");
        }
    }

    private async Task BroadcastStateAsync(Guid matchId, TCG.Core.Models.GameStateSnapshot state)
    {
        var conns = _connections.Get(matchId);
        if (conns is null) return;
        await Clients.Clients(conns.Value.Conn1, conns.Value.Conn2).SendAsync("StateUpdate", state);
    }

    private async Task BroadcastGameOverAsync(Guid matchId, string winnerId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TcgDbContext>();
        var match = await db.Matches.FindAsync(matchId);
        if (match is not null)
        {
            match.Status = MatchStatus.Finished;
            match.WinnerId = winnerId;
            match.EndedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var conns = _connections.Get(matchId);
        if (conns is null) return;
        await Clients.Clients(conns.Value.Conn1, conns.Value.Conn2).SendAsync("GameOver", winnerId);
    }
}

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Core.Services;
using TCG.GameLogic;
using TCG.Server.Data;
using TCG.Server.Hubs;

namespace TCG.Server.Services;

/// <summary>
/// In-memory matchmaking queue. Replace with Redis-backed implementation for production scale.
/// </summary>
public class MatchmakingService : IMatchmakingService
{
    private readonly ConcurrentDictionary<string, (string UserId, Guid DeckId)> _queue = new();
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IGameEngine _gameEngine;
    private readonly IMatchStateStore _matchState;
    private readonly MatchConnectionStore _connections;
    private readonly TcgDbContext _db;

    public MatchmakingService(
        IHubContext<GameHub> hubContext,
        IGameEngine gameEngine,
        IMatchStateStore matchState,
        MatchConnectionStore connections,
        TcgDbContext db)
    {
        _hubContext = hubContext;
        _gameEngine = gameEngine;
        _matchState = matchState;
        _connections = connections;
        _db = db;
    }

    public async Task JoinQueueAsync(string userId, Guid deckId, string connectionId, CancellationToken ct = default)
    {
        _queue[connectionId] = (userId, deckId);

        var entries = _queue.ToArray();
        if (entries.Length < 2) return;

        var (conn1, (u1, d1)) = entries[0];
        var (conn2, (u2, d2)) = entries[1];
        _queue.TryRemove(conn1, out _);
        _queue.TryRemove(conn2, out _);

        var matchId = Guid.NewGuid();
        var state = _gameEngine.CreateNewGame(u1, d1, u2, d2);
        state.MatchId = matchId;

        var match = new Match { Id = matchId, Status = MatchStatus.InProgress, CreatedAt = DateTime.UtcNow };
        match.Participants.Add(new MatchParticipant { Id = Guid.NewGuid(), MatchId = matchId, UserId = u1, DeckId = d1, PlayerIndex = 0, LifeTotal = 20, Status = ParticipantStatus.Active });
        match.Participants.Add(new MatchParticipant { Id = Guid.NewGuid(), MatchId = matchId, UserId = u2, DeckId = d2, PlayerIndex = 1, LifeTotal = 20, Status = ParticipantStatus.Active });
        _db.Matches.Add(match);
        await _db.SaveChangesAsync(ct);

        _matchState.Set(matchId, state);
        _connections.Set(matchId, conn1, conn2, u1, u2);

        await Task.WhenAll(
            _hubContext.Clients.Client(conn1).SendAsync("MatchFound", matchId, d2, ct),
            _hubContext.Clients.Client(conn2).SendAsync("MatchFound", matchId, d1, ct)
        );
    }

    public Task LeaveQueueAsync(string connectionId, CancellationToken ct = default)
    {
        _queue.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }
}

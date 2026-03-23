using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Core.Services;
using TCG.Server.Data;
using TCG.Server.Hubs;

namespace TCG.Server.Services;

public class MatchmakingService : IMatchmakingService
{
    private readonly IMatchmakingQueue _queue;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IGameSetupService _gameSetup;
    private readonly IMatchStateStore _matchState;
    private readonly IMatchConnectionStore _connections;
    private readonly TcgDbContext _db;

    public MatchmakingService(
        IMatchmakingQueue queue,
        IHubContext<GameHub> hubContext,
        IGameSetupService gameSetup,
        IMatchStateStore matchState,
        IMatchConnectionStore connections,
        TcgDbContext db)
    {
        _queue = queue;
        _hubContext = hubContext;
        _gameSetup = gameSetup;
        _matchState = matchState;
        _connections = connections;
        _db = db;
    }

    public async Task JoinQueueAsync(string userId, Guid deckId, string connectionId, CancellationToken ct = default)
    {
        await _queue.EnqueueAsync(connectionId, userId, deckId, ct);

        var pair = await _queue.TryDequeuePairAsync(ct);
        if (pair is null) return;

        var (conn1, u1, d1, conn2, u2, d2) = pair.Value;

        var matchId = Guid.NewGuid();
        var state = await _gameSetup.CreateInitialStateAsync(u1, d1, u2, d2, matchId, ct);

        var match = new Match { Id = matchId, Status = MatchStatus.InProgress, CreatedAt = DateTime.UtcNow };
        match.Participants.Add(new MatchParticipant { Id = Guid.NewGuid(), MatchId = matchId, UserId = u1, DeckId = d1, PlayerIndex = 0, LifeTotal = 20, Status = ParticipantStatus.Active });
        match.Participants.Add(new MatchParticipant { Id = Guid.NewGuid(), MatchId = matchId, UserId = u2, DeckId = d2, PlayerIndex = 1, LifeTotal = 20, Status = ParticipantStatus.Active });
        _db.Matches.Add(match);
        await _db.SaveChangesAsync(ct);

        _matchState.Set(matchId, state);
        _connections.Set(matchId, conn1, conn2, u1, u2);

        await Task.WhenAll(
            _hubContext.Clients.Client(conn1).SendAsync("MatchFound", matchId, d2, ct),
            _hubContext.Clients.Client(conn2).SendAsync("MatchFound", matchId, d1, ct),
            _hubContext.Clients.Client(conn1).SendAsync("StateUpdate", state, ct),
            _hubContext.Clients.Client(conn2).SendAsync("StateUpdate", state, ct)
        );
    }

    public Task LeaveQueueAsync(string connectionId, CancellationToken ct = default) =>
        _queue.RemoveAsync(connectionId);
}

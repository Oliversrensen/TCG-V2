namespace TCG.Server.Services;

public interface IMatchmakingService
{
    Task JoinQueueAsync(string userId, Guid deckId, string connectionId, CancellationToken ct = default);
    Task LeaveQueueAsync(string connectionId, CancellationToken ct = default);
}

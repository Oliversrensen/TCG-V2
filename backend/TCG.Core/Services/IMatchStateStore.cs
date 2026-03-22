using TCG.Core.Models;

namespace TCG.Core.Services;

public interface IMatchStateStore
{
    void Set(Guid matchId, GameStateSnapshot state);
    GameStateSnapshot? Get(Guid matchId);
    void Remove(Guid matchId);
}

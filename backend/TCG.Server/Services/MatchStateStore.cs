using System.Collections.Concurrent;
using TCG.Core.Models;
using TCG.Core.Services;

namespace TCG.Server.Services;

public class MatchStateStore : IMatchStateStore
{
    private readonly ConcurrentDictionary<Guid, GameStateSnapshot> _store = new();

    public void Set(Guid matchId, GameStateSnapshot state) => _store[matchId] = state;
    public GameStateSnapshot? Get(Guid matchId) => _store.TryGetValue(matchId, out var s) ? s : null;
    public void Remove(Guid matchId) => _store.TryRemove(matchId, out _);
}

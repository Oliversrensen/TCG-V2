using System.Text.Json;
using StackExchange.Redis;

namespace TCG.Server.Services;

public interface IMatchmakingQueue
{
    Task EnqueueAsync(string connectionId, string userId, Guid deckId, CancellationToken ct = default);
    Task<(string Conn1, string User1, Guid Deck1, string Conn2, string User2, Guid Deck2)?> TryDequeuePairAsync(CancellationToken ct = default);
    Task RemoveAsync(string connectionId, CancellationToken ct = default);
}

public class RedisMatchmakingQueue : IMatchmakingQueue
{
    private const string QueueKey = "tcg:queue";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;

    public RedisMatchmakingQueue(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public async Task EnqueueAsync(string connectionId, string userId, Guid deckId, CancellationToken ct = default)
    {
        var entry = new QueueEntry { ConnectionId = connectionId, UserId = userId, DeckId = deckId };
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await _db.ListRightPushAsync(QueueKey, json);
    }

    public async Task<(string Conn1, string User1, Guid Deck1, string Conn2, string User2, Guid Deck2)?> TryDequeuePairAsync(CancellationToken ct = default)
    {
        var len = await _db.ListLengthAsync(QueueKey);
        if (len < 2) return null;

        var json1 = await _db.ListLeftPopAsync(QueueKey);
        var json2 = await _db.ListLeftPopAsync(QueueKey);
        if (json1.IsNullOrEmpty || json2.IsNullOrEmpty) return null;

        var e1 = JsonSerializer.Deserialize<QueueEntry>(json1!);
        var e2 = JsonSerializer.Deserialize<QueueEntry>(json2!);
        if (e1 is null || e2 is null) return null;

        return (e1.ConnectionId, e1.UserId, e1.DeckId, e2.ConnectionId, e2.UserId, e2.DeckId);
    }

    public async Task RemoveAsync(string connectionId, CancellationToken ct = default)
    {
        var items = await _db.ListRangeAsync(QueueKey);
        foreach (var item in items)
        {
            if (item.IsNullOrEmpty) continue;
            var entry = JsonSerializer.Deserialize<QueueEntry>(item!);
            if (entry?.ConnectionId == connectionId)
            {
                await _db.ListRemoveAsync(QueueKey, item, 1);
                return;
            }
        }
    }

    private class QueueEntry
    {
        public string ConnectionId { get; set; } = "";
        public string UserId { get; set; } = "";
        public Guid DeckId { get; set; }
    }
}

public class InMemoryMatchmakingQueue : IMatchmakingQueue
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (string User, Guid Deck)> _byConn = new();

    public Task EnqueueAsync(string connectionId, string userId, Guid deckId, CancellationToken ct = default)
    {
        _byConn[connectionId] = (userId, deckId);
        return Task.CompletedTask;
    }

    public Task<(string Conn1, string User1, Guid Deck1, string Conn2, string User2, Guid Deck2)?> TryDequeuePairAsync(CancellationToken ct = default)
    {
        var entries = _byConn.ToArray();
        if (entries.Length < 2) return Task.FromResult<(string, string, Guid, string, string, Guid)?>(null);
        var (conn1, (u1, d1)) = entries[0];
        var (conn2, (u2, d2)) = entries[1];
        _byConn.TryRemove(conn1, out _);
        _byConn.TryRemove(conn2, out _);
        return Task.FromResult<(string, string, Guid, string, string, Guid)?>((conn1, u1, d1, conn2, u2, d2));
    }

    public Task RemoveAsync(string connectionId, CancellationToken ct = default)
    {
        _byConn.TryRemove(connectionId, out _);
        return Task.CompletedTask;
    }
}

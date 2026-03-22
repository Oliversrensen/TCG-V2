using System.Text.Json;
using StackExchange.Redis;
using TCG.Core.Models;
using TCG.Core.Services;

namespace TCG.Server.Services;

public class RedisMatchStateStore : IMatchStateStore
{
    private const string KeyPrefix = "tcg:match:";
    private const int TtlSeconds = 86400; // 24 hours
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;

    public RedisMatchStateStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public void Set(Guid matchId, GameStateSnapshot state)
    {
        var key = KeyPrefix + matchId;
        var json = JsonSerializer.Serialize(state, JsonOptions);
        _db.StringSet(key, json, TimeSpan.FromSeconds(TtlSeconds));
    }

    public GameStateSnapshot? Get(Guid matchId)
    {
        var key = KeyPrefix + matchId;
        var json = _db.StringGet(key);
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<GameStateSnapshot>(json!, JsonOptions);
    }

    public void Remove(Guid matchId) => _db.KeyDelete(KeyPrefix + matchId);
}

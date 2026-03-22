using System.Text.Json;
using StackExchange.Redis;

namespace TCG.Server.Services;

public class RedisMatchConnectionStore : IMatchConnectionStore
{
    private const string MatchPrefix = "tcg:conn:";
    private const string ConnPrefix = "tcg:connid:";
    private const int TtlSeconds = 86400; // 24 hours
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db;

    public RedisMatchConnectionStore(IConnectionMultiplexer redis) => _db = redis.GetDatabase();

    public void Set(Guid matchId, string conn1, string conn2, string user1, string user2)
    {
        var key = MatchPrefix + matchId;
        var data = new ConnData { Conn1 = conn1, Conn2 = conn2, User1 = user1, User2 = user2 };
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var ts = TimeSpan.FromSeconds(TtlSeconds);
        _db.StringSet(key, json, ts);
        _db.StringSet(ConnPrefix + conn1, matchId.ToString(), ts);
        _db.StringSet(ConnPrefix + conn2, matchId.ToString(), ts);
    }

    public (string Conn1, string Conn2)? Get(Guid matchId)
    {
        var key = MatchPrefix + matchId;
        var json = _db.StringGet(key);
        if (json.IsNullOrEmpty) return null;
        var data = JsonSerializer.Deserialize<ConnData>(json!);
        return data is null ? null : (data.Conn1, data.Conn2);
    }

    public Guid? GetMatchId(string connectionId)
    {
        var val = _db.StringGet(ConnPrefix + connectionId);
        return val.IsNullOrEmpty ? null : Guid.TryParse(val, out var g) ? g : null;
    }

    public bool Reconnect(Guid matchId, string userId, string newConnectionId)
    {
        var key = MatchPrefix + matchId;
        var json = _db.StringGet(key);
        if (json.IsNullOrEmpty) return false;
        var data = JsonSerializer.Deserialize<ConnData>(json!);
        if (data is null) return false;
        var oldConn = data.User1 == userId ? data.Conn1 : data.User2 == userId ? data.Conn2 : null;
        if (oldConn is null) return false;

        _db.KeyDelete(ConnPrefix + oldConn);
        if (data.User1 == userId) data.Conn1 = newConnectionId; else data.Conn2 = newConnectionId;
        var updated = JsonSerializer.Serialize(data, JsonOptions);
        _db.StringSet(key, updated, TimeSpan.FromSeconds(TtlSeconds));
        _db.StringSet(ConnPrefix + newConnectionId, matchId.ToString(), TimeSpan.FromSeconds(TtlSeconds));
        return true;
    }

    public void Remove(Guid matchId)
    {
        var key = MatchPrefix + matchId;
        var json = _db.StringGet(key);
        if (!json.IsNullOrEmpty)
        {
            var data = JsonSerializer.Deserialize<ConnData>(json!);
            if (data is not null)
            {
                _db.KeyDelete(ConnPrefix + data.Conn1);
                _db.KeyDelete(ConnPrefix + data.Conn2);
            }
        }
        _db.KeyDelete(key);
    }

    private class ConnData
    {
        public string Conn1 { get; set; } = "";
        public string Conn2 { get; set; } = "";
        public string User1 { get; set; } = "";
        public string User2 { get; set; } = "";
    }
}

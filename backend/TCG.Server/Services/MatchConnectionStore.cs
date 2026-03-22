using System.Collections.Concurrent;

namespace TCG.Server.Services;

/// <summary>
/// Maps match IDs to SignalR connection IDs for broadcasting state updates.
/// Supports reconnection by storing userId so we can update connection when client reconnects.
/// </summary>
public class MatchConnectionStore
{
    private readonly ConcurrentDictionary<Guid, (string Conn1, string Conn2, string User1, string User2)> _matchToConnections = new();
    private readonly ConcurrentDictionary<string, Guid> _connToMatch = new();

    public void Set(Guid matchId, string conn1, string conn2, string user1, string user2)
    {
        _matchToConnections[matchId] = (conn1, conn2, user1, user2);
        _connToMatch[conn1] = matchId;
        _connToMatch[conn2] = matchId;
    }

    public (string Conn1, string Conn2)? Get(Guid matchId) =>
        _matchToConnections.TryGetValue(matchId, out var c) ? (c.Conn1, c.Conn2) : null;

    public Guid? GetMatchId(string connectionId) =>
        _connToMatch.TryGetValue(connectionId, out var m) ? m : null;

    /// <summary>
    /// Update connection for a user who reconnected. Returns true if updated.
    /// </summary>
    public bool Reconnect(Guid matchId, string userId, string newConnectionId)
    {
        if (!_matchToConnections.TryGetValue(matchId, out var c)) return false;
        var oldConn = c.User1 == userId ? c.Conn1 : c.User2 == userId ? c.Conn2 : null;
        if (oldConn is null) return false;
        _connToMatch.TryRemove(oldConn, out _);
        var updated = c.User1 == userId
            ? (newConnectionId, c.Conn2, c.User1, c.User2)
            : (c.Conn1, newConnectionId, c.User1, c.User2);
        _matchToConnections[matchId] = updated;
        _connToMatch[newConnectionId] = matchId;
        return true;
    }

    public void Remove(Guid matchId)
    {
        if (_matchToConnections.TryRemove(matchId, out var c))
        {
            _connToMatch.TryRemove(c.Conn1, out _);
            _connToMatch.TryRemove(c.Conn2, out _);
        }
    }
}

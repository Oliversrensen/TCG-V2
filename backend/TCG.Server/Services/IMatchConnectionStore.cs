namespace TCG.Server.Services;

public interface IMatchConnectionStore
{
    void Set(Guid matchId, string conn1, string conn2, string user1, string user2);
    (string Conn1, string Conn2)? Get(Guid matchId);
    Guid? GetMatchId(string connectionId);
    bool Reconnect(Guid matchId, string userId, string newConnectionId);
    void Remove(Guid matchId);
}

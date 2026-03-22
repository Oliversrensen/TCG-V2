using TCG.Core.Models;

namespace TCG.GameLogic;

public interface IGameEngine
{
    GameStateSnapshot CreateNewGame(string player1Id, Guid deck1Id, string player2Id, Guid deck2Id);
    bool CanPlayCard(GameStateSnapshot state, string playerId, Guid cardId, Guid? targetId);
    GameStateSnapshot PlayCard(GameStateSnapshot state, string playerId, Guid cardId, Guid? targetId);
    bool CanAttack(GameStateSnapshot state, string playerId, string attackerInstanceId, string targetInstanceId);
    GameStateSnapshot Attack(GameStateSnapshot state, string playerId, string attackerInstanceId, string targetInstanceId);
    GameStateSnapshot EndTurn(GameStateSnapshot state, string playerId);
    string? GetWinner(GameStateSnapshot state);
}

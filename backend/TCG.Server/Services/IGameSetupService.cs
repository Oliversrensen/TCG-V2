using TCG.Core.Models;

namespace TCG.Server.Services;

public interface IGameSetupService
{
    /// <summary>Loads decks from DB, shuffles, deals 5 cards each, and builds initial game state.</summary>
    Task<GameStateSnapshot> CreateInitialStateAsync(string player1Id, Guid deck1Id, string player2Id, Guid deck2Id, Guid matchId, CancellationToken ct = default);
}

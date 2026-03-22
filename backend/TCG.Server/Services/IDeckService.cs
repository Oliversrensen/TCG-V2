using TCG.Core.DTOs;
using TCG.Core.Models;

namespace TCG.Server.Services;

public interface IDeckService
{
    Task<IReadOnlyList<DeckDto>> GetDecksAsync(string userId, CancellationToken ct = default);
    Task<DeckDto?> GetDeckAsync(Guid deckId, string userId, CancellationToken ct = default);
    Task<DeckDto> CreateDeckAsync(string userId, CreateDeckRequest request, CancellationToken ct = default);
    Task<DeckDto?> UpdateDeckAsync(Guid deckId, string userId, UpdateDeckRequest request, CancellationToken ct = default);
    Task<bool> DeleteDeckAsync(Guid deckId, string userId, CancellationToken ct = default);
}

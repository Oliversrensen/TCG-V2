using Microsoft.EntityFrameworkCore;
using TCG.Core.DTOs;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Server.Services;

public class CardService : ICardService
{
    private readonly TcgDbContext _db;

    public CardService(TcgDbContext db) => _db = db;

    public async Task<IReadOnlyList<CardDefinitionDto>> GetCardDefinitionsAsync(CancellationToken ct = default)
    {
        return await _db.CardDefinitions
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CardDefinitionDto(
                c.Id,
                c.Name,
                c.Description,
                c.Cost,
                c.Attack,
                c.Defense,
                c.CardType.ToString()
            ))
            .ToListAsync(ct);
    }
}

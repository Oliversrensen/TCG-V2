using Microsoft.EntityFrameworkCore;
using TCG.Core.DTOs;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Server.Services;

public class DeckService : IDeckService
{
    private const int MinDeckSize = 30;
    private const int MaxDeckSize = 60;
    private const int MaxCopiesPerCard = 4;

    private readonly TcgDbContext _db;

    public DeckService(TcgDbContext db) => _db = db;

    public async Task<IReadOnlyList<DeckDto>> GetDecksAsync(string userId, CancellationToken ct = default)
    {
        var decks = await _db.Decks
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .Include(d => d.Slots)
            .ThenInclude(s => s.CardDefinition)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);

        return decks.Select(ToDto).ToList();
    }

    public async Task<DeckDto?> GetDeckAsync(Guid deckId, string userId, CancellationToken ct = default)
    {
        var deck = await _db.Decks
            .AsNoTracking()
            .Include(d => d.Slots)
            .ThenInclude(s => s.CardDefinition)
            .FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId, ct);
        return deck is null ? null : ToDto(deck);
    }

    public async Task<DeckDto> CreateDeckAsync(string userId, CreateDeckRequest request, CancellationToken ct = default)
    {
        var (deck, slots) = await ValidateAndBuildDeckAsync(request, ct);
        deck.UserId = userId;
        deck.CreatedAt = DateTime.UtcNow;
        deck.UpdatedAt = DateTime.UtcNow;

        deck.Slots = slots;
        _db.Decks.Add(deck);
        await _db.SaveChangesAsync(ct);

        return ToDto(deck);
    }

    public async Task<DeckDto?> UpdateDeckAsync(Guid deckId, string userId, UpdateDeckRequest request, CancellationToken ct = default)
    {
        var deck = await _db.Decks.Include(d => d.Slots).FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId, ct);
        if (deck is null) return null;

        var (_, slots) = await ValidateAndBuildDeckAsync(new CreateDeckRequest(request.Name, request.Slots), ct);

        deck.Name = request.Name;
        deck.UpdatedAt = DateTime.UtcNow;
        _db.DeckSlots.RemoveRange(deck.Slots);
        foreach (var slot in slots)
        {
            slot.DeckId = deckId;
            _db.DeckSlots.Add(slot);
        }
        await _db.SaveChangesAsync(ct);

        deck.Slots = slots;
        return ToDto(deck);
    }

    public async Task<bool> DeleteDeckAsync(Guid deckId, string userId, CancellationToken ct = default)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId && d.UserId == userId, ct);
        if (deck is null) return false;
        _db.Decks.Remove(deck);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<(Deck deck, List<DeckSlot> slots)> ValidateAndBuildDeckAsync(CreateDeckRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Deck name is required.");

        var totalCards = request.Slots.Sum(s => s.Quantity);
        if (totalCards < MinDeckSize || totalCards > MaxDeckSize)
            throw new ArgumentException($"Deck must have between {MinDeckSize} and {MaxDeckSize} cards.");

        var cardIds = request.Slots.Select(s => s.CardDefinitionId).Distinct().ToList();
        var validCardList = await _db.CardDefinitions
            .Where(c => cardIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
        var validCards = validCardList.ToHashSet();

        var slots = new List<DeckSlot>();
        foreach (var slotReq in request.Slots)
        {
            if (!validCards.Contains(slotReq.CardDefinitionId))
                throw new ArgumentException($"Invalid card ID: {slotReq.CardDefinitionId}");
            if (slotReq.Quantity < 1 || slotReq.Quantity > MaxCopiesPerCard)
                throw new ArgumentException($"Quantity must be 1-{MaxCopiesPerCard} per card.");
            slots.Add(new DeckSlot
            {
                Id = Guid.NewGuid(),
                CardDefinitionId = slotReq.CardDefinitionId,
                Quantity = slotReq.Quantity,
            });
        }

        var deck = new Deck { Id = Guid.NewGuid(), Name = request.Name.Trim() };
        foreach (var s in slots)
        {
            s.DeckId = deck.Id;
            s.Deck = deck;
        }
        return (deck, slots);
    }

    private static DeckDto ToDto(Deck deck) => new(
        deck.Id,
        deck.Name,
        deck.Slots.Select(s => new DeckSlotDto(s.CardDefinitionId, s.Quantity)).ToList(),
        deck.CreatedAt
    );
}

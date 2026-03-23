using FluentAssertions;
using TCG.Core.DTOs;
using TCG.Core.Models;
using TCG.Server.Data;
using TCG.Server.Services;
using Xunit;

namespace TCG.Tests.Services;

public class DeckServiceTests : IDisposable
{
    private readonly TcgDbContext _db;
    private readonly DeckService _service;
    private const string UserId = "test-user-1";
    private const string OtherUserId = "test-user-2";

    public DeckServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _service = new DeckService(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateDeckAsync_creates_deck_with_valid_slots()
    {
        var slots = BuildSlots(30);
        var request = new CreateDeckRequest("My Deck", slots);

        var deck = await _service.CreateDeckAsync(UserId, request);

        deck.Name.Should().Be("My Deck");
        deck.Slots.Sum(s => s.Quantity).Should().Be(30);
    }

    [Fact]
    public async Task CreateDeckAsync_throws_when_name_empty()
    {
        var slots = BuildSlots(30);
        var request = new CreateDeckRequest("", slots);

        var act = async () => await _service.CreateDeckAsync(UserId, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public async Task CreateDeckAsync_throws_when_too_few_cards()
    {
        var slots = BuildSlots(29);
        var request = new CreateDeckRequest("Deck", slots);

        var act = async () => await _service.CreateDeckAsync(UserId, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*30*60*");
    }

    [Fact]
    public async Task CreateDeckAsync_throws_when_too_many_cards()
    {
        var slots = BuildSlots(61);
        var request = new CreateDeckRequest("Deck", slots);

        var act = async () => await _service.CreateDeckAsync(UserId, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*30*60*");
    }

    [Fact]
    public async Task CreateDeckAsync_throws_when_invalid_card_id()
    {
        var slots = new List<DeckSlotDto>
        {
            new(Guid.NewGuid(), 30)
        };
        var request = new CreateDeckRequest("Deck", slots);

        var act = async () => await _service.CreateDeckAsync(UserId, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Invalid card*");
    }

    [Fact]
    public async Task CreateDeckAsync_throws_when_more_than_4_copies()
    {
        // 30 total cards but 5 copies of one card - must hit max-copies validation (runs after count check)
        var slots = new List<DeckSlotDto>
        {
            new(TestDbContextFactory.CardId1, 5),
            new(TestDbContextFactory.CardId2, 4),
            new(TestDbContextFactory.CardId3, 4),
            new(TestDbContextFactory.CardId4, 4),
            new(TestDbContextFactory.CardId5, 4),
            new(TestDbContextFactory.CardId6, 4),
            new(TestDbContextFactory.CardId7, 4),
            new(TestDbContextFactory.CardId8, 1)
        };
        var request = new CreateDeckRequest("Deck", slots);

        var act = async () => await _service.CreateDeckAsync(UserId, request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*1*4*");
    }

    [Fact]
    public async Task GetDecksAsync_returns_only_user_decks()
    {
        var slots = BuildSlots(30);
        await _service.CreateDeckAsync(UserId, new CreateDeckRequest("User1 Deck", slots));
        await _service.CreateDeckAsync(OtherUserId, new CreateDeckRequest("User2 Deck", slots));

        var decks = await _service.GetDecksAsync(UserId);

        decks.Should().ContainSingle().Which.Name.Should().Be("User1 Deck");
    }

    [Fact]
    public async Task UpdateDeckAsync_returns_null_for_wrong_user()
    {
        var slots = BuildSlots(30);
        var created = await _service.CreateDeckAsync(UserId, new CreateDeckRequest("Deck", slots));
        var updateRequest = new UpdateDeckRequest("Updated", slots);

        var result = await _service.UpdateDeckAsync(created.Id, OtherUserId, updateRequest);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDeckAsync_returns_false_for_wrong_user()
    {
        var slots = BuildSlots(30);
        var created = await _service.CreateDeckAsync(UserId, new CreateDeckRequest("Deck", slots));

        var result = await _service.DeleteDeckAsync(created.Id, OtherUserId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDeckAsync_returns_true_and_removes_deck()
    {
        var slots = BuildSlots(30);
        var created = await _service.CreateDeckAsync(UserId, new CreateDeckRequest("Deck", slots));

        var result = await _service.DeleteDeckAsync(created.Id, UserId);

        result.Should().BeTrue();
        (await _service.GetDeckAsync(created.Id, UserId)).Should().BeNull();
    }

    private static List<DeckSlotDto> BuildSlots(int totalCards)
    {
        var ids = new[]
        {
            TestDbContextFactory.CardId1,
            TestDbContextFactory.CardId2,
            TestDbContextFactory.CardId3,
            TestDbContextFactory.CardId4,
            TestDbContextFactory.CardId5,
            TestDbContextFactory.CardId6,
            TestDbContextFactory.CardId7,
            TestDbContextFactory.CardId8,
            TestDbContextFactory.CardId9,
            TestDbContextFactory.CardId10,
            TestDbContextFactory.CardId11,
            TestDbContextFactory.CardId12,
            TestDbContextFactory.CardId13,
            TestDbContextFactory.CardId14,
            TestDbContextFactory.CardId15,
            TestDbContextFactory.CardId16
        };
        var slots = new List<DeckSlotDto>();
        var remaining = totalCards;
        foreach (var id in ids)
        {
            if (remaining <= 0) break;
            var qty = Math.Min(4, remaining);
            slots.Add(new DeckSlotDto(id, qty));
            remaining -= qty;
        }
        return slots;
    }
}

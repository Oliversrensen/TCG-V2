using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Tests;

public static class TestDbContextFactory
{
    /// <summary>
    /// Pre-defined card IDs for tests. Use these in deck slots.
    /// Need 8+ cards to build 30-card deck (max 4 per card).
    /// </summary>
    public static readonly Guid CardId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CardId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CardId3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid CardId4 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CardId5 = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid CardId6 = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid CardId7 = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid CardId8 = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid CardId9 = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid CardId10 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid CardId11 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid CardId12 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid CardId13 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid CardId14 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid CardId15 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    public static readonly Guid CardId16 = Guid.Parse("10101010-1010-1010-1010-101010101010");

    public static TcgDbContext Create()
    {
        var options = new DbContextOptionsBuilder<TcgDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new TcgDbContext(options);
        SeedCards(db);
        db.SaveChanges();
        return db;
    }

    private static void SeedCards(TcgDbContext db)
    {
        db.CardDefinitions.AddRange(
            new CardDefinition
            {
                Id = CardId1,
                Name = "Test Card 1",
                Description = "For testing",
                Cost = 1,
                Attack = 1,
                Defense = 1,
                CardType = CardType.Creature,
                CreatedAt = DateTime.UtcNow
            },
            new CardDefinition
            {
                Id = CardId2,
                Name = "Test Card 2",
                Description = "For testing",
                Cost = 2,
                Attack = 2,
                Defense = 2,
                CardType = CardType.Creature,
                CreatedAt = DateTime.UtcNow
            },
            new CardDefinition
            {
                Id = CardId3,
                Name = "Test Card 3",
                Description = "For testing",
                Cost = 3,
                CardType = CardType.Spell,
                CreatedAt = DateTime.UtcNow
            },
            new CardDefinition { Id = CardId4, Name = "Test Card 4", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId5, Name = "Test Card 5", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId6, Name = "Test Card 6", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId7, Name = "Test Card 7", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId8, Name = "Test Card 8", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId9, Name = "Test Card 9", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId10, Name = "Test Card 10", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId11, Name = "Test Card 11", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId12, Name = "Test Card 12", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId13, Name = "Test Card 13", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId14, Name = "Test Card 14", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId15, Name = "Test Card 15", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow },
            new CardDefinition { Id = CardId16, Name = "Test Card 16", Description = "", Cost = 1, CardType = CardType.Creature, CreatedAt = DateTime.UtcNow }
        );
    }
}

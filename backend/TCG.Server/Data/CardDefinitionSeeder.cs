using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;

namespace TCG.Server.Data;

public static class CardDefinitionSeeder
{
    public static async Task SeedAsync(TcgDbContext db)
    {
        if (await db.CardDefinitions.AnyAsync())
            return;

        var cards = new List<CardDefinition>
        {
            new() { Id = Guid.NewGuid(), Name = "Fire Drake", Description = "A fierce dragon.", Cost = 3, Attack = 3, Defense = 2, CardType = CardType.Creature },
            new() { Id = Guid.NewGuid(), Name = "Water Elemental", Description = "Summons a water spirit.", Cost = 2, Attack = 2, Defense = 3, CardType = CardType.Creature },
            new() { Id = Guid.NewGuid(), Name = "Lightning Bolt", Description = "Deal 3 damage.", Cost = 1, CardType = CardType.Spell },
            new() { Id = Guid.NewGuid(), Name = "Healing Touch", Description = "Restore 5 life.", Cost = 2, CardType = CardType.Spell },
            new() { Id = Guid.NewGuid(), Name = "Knight", Description = "A loyal knight.", Cost = 2, Attack = 2, Defense = 2, CardType = CardType.Creature },
        };

        foreach (var c in cards)
            c.CreatedAt = DateTime.UtcNow;

        db.CardDefinitions.AddRange(cards);
        await db.SaveChangesAsync();
    }
}

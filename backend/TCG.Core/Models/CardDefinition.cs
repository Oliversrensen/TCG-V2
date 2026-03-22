namespace TCG.Core.Models;

public class CardDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Cost { get; set; }
    public int? Attack { get; set; }
    public int? Defense { get; set; }
    public CardType CardType { get; set; }
    public string? EffectsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum CardType
{
    Creature = 0,
    Spell = 1,
}

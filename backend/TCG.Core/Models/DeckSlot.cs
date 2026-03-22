namespace TCG.Core.Models;

public class DeckSlot
{
    public Guid Id { get; set; }
    public Guid DeckId { get; set; }
    public Guid CardDefinitionId { get; set; }
    public int Quantity { get; set; }

    public Deck Deck { get; set; } = null!;
    public CardDefinition CardDefinition { get; set; } = null!;
}

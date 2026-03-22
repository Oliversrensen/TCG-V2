namespace TCG.Core.Models;

public class Deck
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Links to neon_auth.user id
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<DeckSlot> Slots { get; set; } = new List<DeckSlot>();
}

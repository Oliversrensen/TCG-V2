namespace TCG.Core.Models;

public class MatchParticipant
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid DeckId { get; set; }
    public int PlayerIndex { get; set; }
    public int LifeTotal { get; set; }
    public ParticipantStatus Status { get; set; }

    public Match Match { get; set; } = null!;
}

public enum ParticipantStatus
{
    Active = 0,
    Defeated = 1,
}

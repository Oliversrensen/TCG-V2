namespace TCG.Core.Models;

public class Match
{
    public Guid Id { get; set; }
    public MatchStatus Status { get; set; }
    public string? WinnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
}

public enum MatchStatus
{
    Waiting = 0,
    InProgress = 1,
    Finished = 2,
}

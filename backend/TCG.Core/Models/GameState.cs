namespace TCG.Core.Models;

/// <summary>
/// Represents the authoritative game state for a match.
/// Stored as JSON in Redis during active matches; persisted to DB for replays.
/// </summary>
public class GameStateSnapshot
{
    public Guid MatchId { get; set; }
    public int CurrentTurn { get; set; }
    public string CurrentPlayerId { get; set; } = string.Empty;
    public GamePhase Phase { get; set; }
    public Dictionary<string, PlayerBoardState> Players { get; set; } = new();
}

public class PlayerBoardState
{
    public string UserId { get; set; } = string.Empty;
    public int LifeTotal { get; set; }
    public List<string> Hand { get; set; } = new();
    public List<BoardCreature> Board { get; set; } = new();
}

public class BoardCreature
{
    public string InstanceId { get; set; } = string.Empty;
    public Guid CardDefinitionId { get; set; }
    public int CurrentAttack { get; set; }
    public int CurrentDefense { get; set; }
}

public enum GamePhase
{
    Draw = 0,
    Main = 1,
    Attack = 2,
    End = 3,
}

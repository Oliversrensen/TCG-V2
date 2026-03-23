namespace TCG.Core.Models;

/// <summary>Game-specific user profile. Links to neon_auth.user.id.</summary>
public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

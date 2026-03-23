using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly TcgDbContext _db;

    public UsersController(TcgDbContext db) => _db = db;

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var profile = await _db.UserProfiles.FindAsync(new object[] { userId }, ct);
        if (profile is null)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? email ?? userId;
            profile = new UserProfile
            {
                Id = userId,
                DisplayName = name,
                Email = email,
                Wins = 0,
                Losses = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            id = profile.Id,
            displayName = profile.DisplayName,
            email = profile.Email,
            wins = profile.Wins,
            losses = profile.Losses,
            createdAt = profile.CreatedAt
        });
    }
}

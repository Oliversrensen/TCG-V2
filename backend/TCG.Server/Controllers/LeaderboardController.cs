using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCG.Core.Models;
using TCG.Server.Data;

namespace TCG.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly TcgDbContext _db;

    public LeaderboardController(TcgDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetLeaderboard([FromQuery] int top = 10, CancellationToken ct = default)
    {
        var finished = await _db.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished && m.WinnerId != null)
            .Select(m => m.WinnerId)
            .ToListAsync(ct);

        var wins = finished
            .GroupBy(id => id!)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kv => kv.Value)
            .Take(Math.Clamp(top, 1, 100))
            .Select((kv, i) => new { rank = i + 1, userId = kv.Key, wins = kv.Value })
            .ToList();

        return Ok(new { entries = wins });
    }
}

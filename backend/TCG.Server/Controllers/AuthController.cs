using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TCG.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    /// <summary>Returns Neon Auth URL for the client.</summary>
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var neonAuthUrl = _config["NEON_AUTH_URL"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(neonAuthUrl))
            return StatusCode(500, new { error = "NEON_AUTH_URL is not configured" });
        return Ok(new { neonAuthUrl });
    }
}

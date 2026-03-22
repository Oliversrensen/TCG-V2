using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TCG.Server.Services;

namespace TCG.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CardsController : ControllerBase
{
    private readonly ICardService _cardService;

    public CardsController(ICardService cardService) => _cardService = cardService;

    [HttpGet("definitions")]
    public async Task<ActionResult> GetDefinitions(CancellationToken ct)
    {
        var cards = await _cardService.GetCardDefinitionsAsync(ct);
        return Ok(cards);
    }
}

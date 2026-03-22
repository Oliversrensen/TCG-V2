using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TCG.Core.DTOs;
using TCG.Server.Services;

namespace TCG.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DecksController : ControllerBase
{
    private readonly IDeckService _deckService;

    public DecksController(IDeckService deckService) => _deckService = deckService;

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? throw new UnauthorizedAccessException("User ID not found");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeckDto>>> GetDecks(CancellationToken ct)
    {
        var decks = await _deckService.GetDecksAsync(UserId, ct);
        return Ok(decks);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeckDto>> GetDeck(Guid id, CancellationToken ct)
    {
        var deck = await _deckService.GetDeckAsync(id, UserId, ct);
        return deck is null ? NotFound() : Ok(deck);
    }

    [HttpPost]
    public async Task<ActionResult<DeckDto>> CreateDeck([FromBody] CreateDeckRequest request, CancellationToken ct)
    {
        try
        {
            var deck = await _deckService.CreateDeckAsync(UserId, request, ct);
            return CreatedAtAction(nameof(GetDeck), new { id = deck.Id }, deck);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DeckDto>> UpdateDeck(Guid id, [FromBody] UpdateDeckRequest request, CancellationToken ct)
    {
        try
        {
            var deck = await _deckService.UpdateDeckAsync(id, UserId, request, ct);
            return deck is null ? NotFound() : Ok(deck);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteDeck(Guid id, CancellationToken ct)
    {
        var deleted = await _deckService.DeleteDeckAsync(id, UserId, ct);
        return deleted ? NoContent() : NotFound();
    }
}

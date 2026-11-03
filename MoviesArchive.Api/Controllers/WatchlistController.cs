using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class WatchlistController : Controller
{
    private readonly IWatchlistService _watchlistService;

    public WatchlistController(
        IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    // Filmes da watchlist do usuário logado, do mais recente ao mais antigo
    [HttpGet("api/watchlist/me")]
    public async Task<IActionResult> GetMyWatchlistAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        return Ok(await _watchlistService.GetMoviesAsync(userId.Value));
    }

    [HttpPut("api/watchlist/{movieId:guid}")]
    public async Task<IActionResult> AddAsync([FromRoute] Guid movieId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _watchlistService.AddAsync(userId.Value, movieId);
            return NoContent();
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("api/watchlist/{movieId:guid}")]
    public async Task<IActionResult> RemoveAsync([FromRoute] Guid movieId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        await _watchlistService.RemoveAsync(userId.Value, movieId);
        return NoContent();
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

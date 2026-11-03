using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.History.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MoviesArchive.Api.Controllers;

// Histórico de visualizações: só o próprio usuário registra e consulta o seu
[ApiController]
[Authorize]
public class HistoryController : Controller
{
    private const int MaxPageSize = 50;

    private readonly IWatchHistoryService _watchHistoryService;

    public HistoryController(
        IWatchHistoryService watchHistoryService)
    {
        _watchHistoryService = watchHistoryService;
    }

    // Chamado pelo botão de play da ficha do filme
    [HttpPost("api/history")]
    public async Task<IActionResult> RecordAsync([FromBody] RecordWatchRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var entry = await _watchHistoryService.RecordAsync(userId.Value, request);
            return Ok(entry);
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { field = "movieId", message = ex.Message });
        }
    }

    // Linha do tempo do usuário logado, do mais recente para o mais antigo
    [HttpGet("api/history/me")]
    public async Task<IActionResult> GetMyHistoryAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var history = await _watchHistoryService.GetByUserIdAsync(
            userId.Value,
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, MaxPageSize));

        return Ok(history);
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

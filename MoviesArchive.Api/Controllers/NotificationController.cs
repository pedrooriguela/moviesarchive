using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Controllers;

// Avisos de curtidas e respostas no que o usuário escreveu. Cada um só vê os seus.
[ApiController]
[Authorize]
public class NotificationController : Controller
{
    private const int MaxPageSize = 50;

    private readonly INotificationService _notificationService;

    public NotificationController(
        INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // Mais recentes primeiro, com a contagem de não lidas
    [HttpGet("api/notifications/me")]
    public async Task<IActionResult> GetMyAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        return Ok(await _notificationService.GetMyAsync(userId.Value, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize)));
    }

    // Só o número, para o contador do sino (consultado de tempos em tempos)
    [HttpGet("api/notifications/me/unread-count")]
    public async Task<IActionResult> GetUnreadCountAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        return Ok(new { count = await _notificationService.CountUnreadAsync(userId.Value) });
    }

    [HttpPost("api/notifications/me/read-all")]
    public async Task<IActionResult> MarkAllReadAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        await _notificationService.MarkAllReadAsync(userId.Value);
        return NoContent();
    }

    [HttpPost("api/notifications/{notificationId:guid}/read")]
    public async Task<IActionResult> MarkReadAsync([FromRoute] Guid notificationId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _notificationService.MarkReadAsync(userId.Value, notificationId);
            return NoContent();
        }
        catch (NotificationNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

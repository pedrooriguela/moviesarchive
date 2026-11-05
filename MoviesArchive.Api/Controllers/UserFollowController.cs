using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class UserFollowController(IUserFollowRepository repository) : ControllerBase
{
    private Guid? ViewerId => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet("api/users")]
    public async Task<IActionResult> FindAsync([FromQuery] string? search, [FromQuery] string scope = "all",
        [FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (ViewerId is not Guid viewer || !await repository.IsActiveUserAsync(viewer)) return Unauthorized();
        if (scope is not ("all" or "following" or "followers") || offset < 0 || limit < 1 || limit > 50)
            return BadRequest(new { message = "Filtro ou paginação inválidos." });
        if (search?.Length > 100) return BadRequest(new { message = "Busque por até 100 caracteres." });
        return Ok(await repository.FindAsync(viewer, search?.Trim() ?? "", scope, offset, limit));
    }

    [HttpPut("api/users/{userId:guid}/follow")]
    public async Task<IActionResult> FollowAsync(Guid userId)
    {
        if (ViewerId is not Guid viewer || !await repository.IsActiveUserAsync(viewer)) return Unauthorized();
        if (viewer == userId) return BadRequest(new { message = "Você não pode seguir a si mesma(o)." });
        if (!await repository.IsActiveUserAsync(userId))
            return NotFound(new { message = "Usuário não encontrado." });
        await repository.FollowAsync(viewer, userId);
        return NoContent();
    }

    [HttpDelete("api/users/{userId:guid}/follow")]
    public async Task<IActionResult> UnfollowAsync(Guid userId)
    {
        if (ViewerId is not Guid viewer || !await repository.IsActiveUserAsync(viewer)) return Unauthorized();
        await repository.UnfollowAsync(viewer, userId);
        return NoContent();
    }
}

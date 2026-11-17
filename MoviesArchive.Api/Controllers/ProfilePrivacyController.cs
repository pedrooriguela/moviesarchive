using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class ProfilePrivacyController(IProfilePrivacyRepository profiles, IUserFollowRepository users) : ControllerBase
{
    private Guid? ViewerId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpPut("api/users/me/privacy")]
    public async Task<IActionResult> SetPrivacyAsync([FromBody] PrivacyRequestDto request)
    {
        if (ViewerId is not Guid viewer) return Unauthorized();
        if (request.IsPrivate == null) return BadRequest(new { message = "Informe a privacidade do perfil." });
        return await profiles.SetPrivacyAsync(viewer, request.IsPrivate.Value) ? NoContent() : Unauthorized();
    }

    [HttpGet("api/users/{ownerId:guid}/profile")]
    public async Task<IActionResult> GetProfileAsync(Guid ownerId, [FromQuery] int listsPage = 1, [FromQuery] int reviewsPage = 1)
    {
        if (ViewerId is not Guid viewer || !await users.IsActiveUserAsync(viewer)) return Unauthorized();
        if (listsPage < 1 || reviewsPage < 1 || listsPage > 100000 || reviewsPage > 100000)
            return BadRequest(new { message = "Página inválida." });
        var result = await profiles.GetProfileAsync(ownerId, viewer, listsPage, reviewsPage);
        return result == null ? NotFound(new { message = "Usuário não encontrado." }) : Ok(result);
    }

    [HttpGet("api/users/me/follow-requests")]
    public async Task<IActionResult> GetRequestsAsync([FromQuery] int offset = 0, [FromQuery] int limit = 20)
    {
        if (ViewerId is not Guid viewer || !await users.IsActiveUserAsync(viewer)) return Unauthorized();
        if (offset < 0 || limit < 1 || limit > 50) return BadRequest(new { message = "Paginação inválida." });
        return Ok(await profiles.GetRequestsAsync(viewer, offset, limit));
    }

    [HttpPut("api/users/me/follow-requests/{followerId:guid}")]
    public async Task<IActionResult> AcceptAsync(Guid followerId)
    {
        if (ViewerId is not Guid viewer || !await users.IsActiveUserAsync(viewer)) return Unauthorized();
        return await profiles.AcceptRequestAsync(viewer, followerId) ? NoContent() : NotFound();
    }

    [HttpDelete("api/users/me/follow-requests/{followerId:guid}")]
    public async Task<IActionResult> DeclineAsync(Guid followerId)
    {
        if (ViewerId is not Guid viewer || !await users.IsActiveUserAsync(viewer)) return Unauthorized();
        await profiles.DeclineRequestAsync(viewer, followerId);
        return NoContent();
    }
}

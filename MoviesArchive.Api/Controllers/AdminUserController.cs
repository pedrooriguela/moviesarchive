using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Api.Auth;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin.Dtos;

namespace MoviesArchive.Api.Controllers;

// Painel do administrador: quem é administrador. Pode haver vários; um admin promove outros
// e tira o acesso deles, mas não o próprio (assim sempre sobra pelo menos um).
[ApiController]
[Authorize(Policy = AdminPolicy.Name)]
public class AdminUserController : Controller
{
    private const int MaxResults = 20;

    private readonly IUserRepository _userRepository;

    public AdminUserController(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Sem query: os administradores atuais. Com query: busca por nome ou e-mail.
    [HttpGet("api/admin/users")]
    public async Task<IActionResult> SearchAsync([FromQuery] string? query)
    {
        var userId = GetAuthenticatedUserId();
        var onlyAdmins = string.IsNullOrWhiteSpace(query);
        var users = await _userRepository.SearchAsync(query, onlyAdmins, onlyAdmins ? 100 : MaxResults);

        return Ok(users.Select(user => new AdminUserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhotoUrl = user.PhotoData != null ? $"/api/users/{user.Id}/photo" : null,
            IsAdmin = user.IsAdmin,
            IsMe = user.Id == userId,
        }));
    }

    [HttpPut("api/admin/users/{userId:guid}/admin")]
    public async Task<IActionResult> PromoteAsync([FromRoute] Guid userId)
    {
        if (!await _userRepository.SetAdminAsync(userId, isAdmin: true))
            return NotFound(new { message = new UserNotFoundException().Message });

        return NoContent();
    }

    [HttpDelete("api/admin/users/{userId:guid}/admin")]
    public async Task<IActionResult> DemoteAsync([FromRoute] Guid userId)
    {
        if (userId == GetAuthenticatedUserId())
            return Conflict(new { code = "admin-self", message = new AdminSelfDemotionException().Message });

        if (!await _userRepository.SetAdminAsync(userId, isAdmin: false))
            return NotFound(new { message = new UserNotFoundException().Message });

        return NoContent();
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

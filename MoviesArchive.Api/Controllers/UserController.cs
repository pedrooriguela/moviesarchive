using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Dtos;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Api.Controllers;

[ApiController]
public class UserController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IUserLoginService _userLoginService;
    private readonly IPasswordResetService _passwordResetService;

    public UserController(
        IUserRepository userRepository,
        IUserLoginService userLoginService,
        IJwtService jwtService,
        IPasswordResetService passwordResetService)
    {
        _userRepository = userRepository;
        _userLoginService = userLoginService;
        _jwtService = jwtService;
        _passwordResetService = passwordResetService;
    }

    [HttpPost("api/users/signup")]
    public async Task<IActionResult> SignUpAsync([FromBody] CreateUserRequestDto request)
    {
        try
        {
        await _userLoginService.CreateAccountAsync(request);
        return Ok();
        }
        catch (UsernameAlreadyInUseException ex)
        {
            return Conflict(new { field = "username", message = ex.Message });
        }
        catch (EmailAlreadyInUseException ex)
        {
            return Conflict(new { field = "email", message = ex.Message });
        }
    }

    [HttpGet("api/users/signin")]
    public async Task<IActionResult> SignInAsync([FromQuery] LogInUserRequestDto request)
    {
        try
        {
            var token = await _userLoginService.LogInAsync(request);
            return Ok(new { token });
        }
        catch ( WrongUsernameOrPasswordException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("api/users/me")]
    public async Task<IActionResult> GetMyProfileAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound();

        return Ok(new UserProfileResponseDto
        {
            Name = user.Name,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl
        });
    }

    [Authorize]
    [HttpPut("api/users/me")]
    public async Task<IActionResult> UpdateMyProfileAsync([FromBody] UpdateUserProfileRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var name = request.Name.Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "Nome de usuário é obrigatório." });

        var existingUser = await _userRepository.GetByUsernameAsync(name);
        if (existingUser != null && existingUser.Id != userId.Value)
            return Conflict(new { field = "name", message = "Esse nome de usuário já está em uso." });

        var photoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim();
        if (photoUrl != null &&
            (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var parsedPhotoUrl) ||
             (parsedPhotoUrl.Scheme != Uri.UriSchemeHttp && parsedPhotoUrl.Scheme != Uri.UriSchemeHttps)))
        {
            return BadRequest(new { message = "A URL da foto deve usar HTTP ou HTTPS." });
        }

        var updated = await _userRepository.UpdateProfileAsync(userId.Value, name, photoUrl);
        if (!updated)
            return NotFound();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return Ok(new UserProfileResponseDto
        {
            Name = user!.Name,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl
        });
    }

    private Guid? GetAuthenticatedUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }


[HttpPost("api/users/forgot-password")]
    public async Task<IActionResult> SendResetPasswordLinkAsync([FromBody] string email)
    {
        try
        {
            await _passwordResetService.AskPasswordResetAsync(email);
            return Ok();
        }
        catch(Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("api/users/reset-password")]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto request)
    {
        try
        {
            await _passwordResetService.ResetPasswordAsync(request.NewPassword, request.Token);
            return Ok();
        }
        catch(Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

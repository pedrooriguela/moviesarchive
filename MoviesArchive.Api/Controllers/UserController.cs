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
    private const long MaxPhotoSize = 5 * 1024 * 1024;
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
            PhotoUrl = GetPhotoUrl(user.Id, user.PhotoData != null)
        });
    }

    [AllowAnonymous]
    [HttpGet("api/users/{userId:guid}/photo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetProfilePhotoAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.PhotoData == null || user.PhotoContentType == null)
            return NotFound();

        return File(user.PhotoData, user.PhotoContentType);
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

        var updated = await _userRepository.UpdateProfileAsync(userId.Value, name);
        if (!updated)
            return NotFound();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        return Ok(new UserProfileResponseDto
        {
            Name = user!.Name,
            Email = user.Email,
            PhotoUrl = GetPhotoUrl(user.Id, user.PhotoData != null)
        });
    }

    [Authorize]
    [HttpPut("api/users/me/photo")]
    [RequestSizeLimit(MaxPhotoSize + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxPhotoSize + 64 * 1024)]
    public async Task<IActionResult> UpdateMyProfilePhotoAsync([FromForm] IFormFile? photo)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        if (photo == null || photo.Length == 0)
            return BadRequest(new { message = "Selecione uma foto para enviar." });

        if (photo.Length > MaxPhotoSize)
            return BadRequest(new { message = "A foto deve ter no máximo 5 MB." });

        await using var buffer = new MemoryStream();
        await photo.CopyToAsync(buffer);
        var photoData = buffer.ToArray();
        var contentType = DetectPhotoContentType(photoData);
        if (contentType == null)
            return BadRequest(new { message = "Envie uma imagem JPEG, PNG ou WebP válida." });

        var updated = await _userRepository.UpdateProfilePhotoAsync(userId.Value, photoData, contentType);
        if (!updated)
            return NotFound();

        return Ok(new { photoUrl = GetPhotoUrl(userId.Value, hasPhoto: true) });
    }

    [Authorize]
    [HttpDelete("api/users/me/photo")]
    public async Task<IActionResult> RemoveMyProfilePhotoAsync()
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        var removed = await _userRepository.RemoveProfilePhotoAsync(userId.Value);
        if (!removed)
            return NotFound();

        return NoContent();
    }

    private static string? GetPhotoUrl(Guid userId, bool hasPhoto) =>
        hasPhoto ? $"/api/users/{userId}/photo" : null;

    private static string? DetectPhotoContentType(byte[] data)
    {
        if (data.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return "image/jpeg";

        if (data.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";

        if (data.Length >= 12 &&
            data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
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

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

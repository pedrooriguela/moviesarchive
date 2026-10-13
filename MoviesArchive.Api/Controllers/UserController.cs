using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;
using MoviesArchive.Infra.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MoviesArchive.Api.Controllers;

[ApiController]
public class UserController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly IUserLoginService _userLoginService;

    public UserController(
        IUserRepository userRepository,
        IUserLoginService userLoginService)
    {
        _userRepository = userRepository;
        _userLoginService = userLoginService;
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
    public IActionResult SignInAsync([FromQuery] LogInUserRequestDto request)
    {
        try
        {
            var token = _userLoginService.LogIn(request);
            return Ok(new { token });
        }
        catch ( WrongUsernameOrPasswordException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }


    [Authorize]
    [HttpPost("api/users/changePassword")]
    public IActionResult CreateChangePassworRequest()
    {
        
    }
}

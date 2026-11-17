using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Api.Auth;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Controllers;

// Renovação e encerramento da sessão. Não usa [Authorize]: o JWT pode já ter vencido,
// quem identifica a sessão é o refresh token do cookie.
[ApiController]
public class AuthController : Controller
{
    private readonly IAuthTokenService _authTokenService;

    public AuthController(
        IAuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    // Troca o refresh token do cookie por um novo e devolve um JWT novo: { token }
    [HttpPost("api/auth/refresh")]
    public async Task<IActionResult> RefreshAsync()
    {
        try
        {
            var tokens = await _authTokenService.RefreshAsync(RefreshTokenCookie.Read(Request) ?? string.Empty);
            RefreshTokenCookie.Write(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return Ok(new { token = tokens.AccessToken });
        }
        catch (InvalidRefreshTokenException ex)
        {
            RefreshTokenCookie.Clear(Response);
            return Unauthorized(new { code = "session-expired", message = ex.Message });
        }
    }

    // Sair: o refresh token deixa de valer e o cookie é apagado
    [HttpPost("api/auth/logout")]
    public async Task<IActionResult> LogoutAsync()
    {
        await _authTokenService.RevokeAsync(RefreshTokenCookie.Read(Request) ?? string.Empty);
        RefreshTokenCookie.Clear(Response);
        return NoContent();
    }
}

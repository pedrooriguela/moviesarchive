using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Api.Auth;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;

namespace MoviesArchive.Api.Controllers;

// Painel do administrador: cadastro, edição, remoção e restauração de filmes.
// Remover não apaga: o filme some para os usuários e continua nos históricos.
[ApiController]
[Authorize(Policy = AdminPolicy.Name)]
public class AdminMovieController : Controller
{
    private const int MaxPageSize = 50;

    private readonly IMovieAdminService _movieAdminService;

    public AdminMovieController(
        IMovieAdminService movieAdminService)
    {
        _movieAdminService = movieAdminService;
    }

    // status: "all" (padrão), "active" (no catálogo) ou "removed"
    [HttpGet("api/admin/movies")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? query,
        [FromQuery] string? status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var parsedStatus = status?.ToLowerInvariant() switch
        {
            null or "" or "all" => AdminMovieStatus.All,
            "active" => AdminMovieStatus.Active,
            "removed" => AdminMovieStatus.Removed,
            _ => (AdminMovieStatus?)null,
        };

        if (parsedStatus == null)
            return BadRequest(new { field = "status", message = "Situação inválida. Use all, active ou removed." });

        return Ok(await _movieAdminService.SearchAsync(
            query?.Trim(), parsedStatus.Value, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize)));
    }

    [HttpGet("api/admin/movies/{movieId:guid}")]
    public async Task<IActionResult> GetAsync([FromRoute] Guid movieId)
    {
        try
        {
            return Ok(await _movieAdminService.GetAsync(movieId));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("api/admin/movies")]
    public async Task<IActionResult> CreateAsync([FromBody] SaveMovieRequestDto request)
    {
        try
        {
            return Ok(await _movieAdminService.CreateAsync(request));
        }
        catch (InvalidMovieDataException ex)
        {
            return InvalidField(ex);
        }
        catch (MovieAlreadyExistsException ex)
        {
            return Conflict(new { field = "title", message = ex.Message });
        }
    }

    [HttpPut("api/admin/movies/{movieId:guid}")]
    public async Task<IActionResult> UpdateAsync([FromRoute] Guid movieId, [FromBody] SaveMovieRequestDto request)
    {
        try
        {
            return Ok(await _movieAdminService.UpdateAsync(movieId, request));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidMovieDataException ex)
        {
            return InvalidField(ex);
        }
        catch (MovieAlreadyExistsException ex)
        {
            return Conflict(new { field = "title", message = ex.Message });
        }
    }

    // Remove do catálogo (soft delete). Devolve o filme já marcado como removido.
    [HttpDelete("api/admin/movies/{movieId:guid}")]
    public async Task<IActionResult> RemoveAsync([FromRoute] Guid movieId)
    {
        try
        {
            return Ok(await _movieAdminService.RemoveAsync(movieId));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("api/admin/movies/{movieId:guid}/restore")]
    public async Task<IActionResult> RestoreAsync([FromRoute] Guid movieId)
    {
        try
        {
            return Ok(await _movieAdminService.RestoreAsync(movieId));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MovieAlreadyExistsException ex)
        {
            return Conflict(new { field = "title", message = ex.Message });
        }
    }

    // Mesmo formato dos erros de validação do [ApiController], para o front mostrar no campo
    private BadRequestObjectResult InvalidField(InvalidMovieDataException ex) =>
        BadRequest(new { errors = new Dictionary<string, string[]> { [ex.Field] = [ex.Message] } });
}

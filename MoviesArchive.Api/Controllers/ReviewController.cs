using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class ReviewController : Controller
{
    private const int MaxPageSize = 50;

    private readonly IReviewService _reviewService;

    public ReviewController(
        IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // Avaliações de um filme (mais recentes primeiro), com média e distribuição das notas
    [HttpGet("api/movies/{movieId:guid}/reviews")]
    public async Task<IActionResult> GetMovieReviewsAsync(
        [FromRoute] Guid movieId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            return Ok(await _reviewService.GetMovieReviewsAsync(movieId, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize)));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Situação do filme para o usuário logado: watchlist e avaliação própria
    [HttpGet("api/movies/{movieId:guid}/me")]
    public async Task<IActionResult> GetMyStateAsync([FromRoute] Guid movieId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            return Ok(await _reviewService.GetUserStateAsync(movieId, userId.Value));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Cria ou substitui a avaliação do usuário logado
    [HttpPut("api/movies/{movieId:guid}/review")]
    public async Task<IActionResult> UpsertMyReviewAsync([FromRoute] Guid movieId, [FromBody] UpsertReviewRequestDto request)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            return Ok(await _reviewService.UpsertAsync(movieId, userId.Value, request));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidReviewRatingException ex)
        {
            return BadRequest(new { field = "rating", message = ex.Message });
        }
        catch (ReviewCommentTooLongException ex)
        {
            return BadRequest(new { field = "comment", message = ex.Message });
        }
    }

    [HttpDelete("api/movies/{movieId:guid}/review")]
    public async Task<IActionResult> DeleteMyReviewAsync([FromRoute] Guid movieId)
    {
        var userId = GetAuthenticatedUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _reviewService.DeleteAsync(movieId, userId.Value);
            return NoContent();
        }
        catch (ReviewNotFoundException ex)
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

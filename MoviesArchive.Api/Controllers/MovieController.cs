using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class MovieController : Controller
{
    private const int MaxResults = 50;

    private readonly IMovieRepository _movieRepository;

    public MovieController(
        IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    [HttpGet("api/movies")]
    public async Task<IActionResult> SearchAsync([FromQuery] string? search, [FromQuery] int limit = 20)
    {
        var movies = await _movieRepository.SearchByTitleAsync(search, Math.Clamp(limit, 1, MaxResults));

        return Ok(movies.Select(movie => new MovieResponseDto
        {
            Id = movie.Id,
            Title = movie.Title,
            ReleaseDate = movie.ReleaseDate
        }));
    }
}

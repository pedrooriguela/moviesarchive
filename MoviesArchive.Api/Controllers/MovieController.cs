using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace MoviesArchive.Api.Controllers;

[ApiController]
[Authorize]
public class MovieController : Controller
{
    private const int MaxResults = 50;
    private const int MinYear = 1880;
    private const int MaxYear = 2100;

    private readonly IMovieRepository _movieRepository;
    private readonly IMovieCatalogService _movieCatalogService;

    public MovieController(
        IMovieRepository movieRepository,
        IMovieCatalogService movieCatalogService)
    {
        _movieRepository = movieRepository;
        _movieCatalogService = movieCatalogService;
    }

    // Busca simples por título, usada no seletor de filmes das listas
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

    // Busca completa do catálogo: título, diretor e gênero, com filtros, ordenação e paginação
    [HttpGet("api/movies/catalog")]
    public async Task<IActionResult> SearchCatalogAsync(
        [FromQuery] string? query,
        [FromQuery] string? language,
        [FromQuery] int? yearFrom,
        [FromQuery] int? yearTo,
        [FromQuery] string? sort = "default",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        if (yearFrom is < MinYear or > MaxYear)
            return BadRequest(new { field = "yearFrom", message = "Ano inicial fora do intervalo permitido." });

        if (yearTo is < MinYear or > MaxYear)
            return BadRequest(new { field = "yearTo", message = "Ano final fora do intervalo permitido." });

        if (yearFrom > yearTo)
            return BadRequest(new { field = "yearFrom", message = "O ano inicial não pode ser maior que o ano final." });

        var filter = new MovieSearchFilter(
            Query: query?.Trim(),
            Language: language?.Trim(),
            YearFrom: yearFrom,
            YearTo: yearTo,
            Sort: sort ?? "default",
            Page: Math.Max(1, page),
            PageSize: Math.Clamp(pageSize, 1, MaxResults));

        return Ok(await _movieCatalogService.SearchAsync(filter));
    }

    // Página inicial: filme em destaque e prateleiras
    [HttpGet("api/movies/home")]
    public async Task<IActionResult> GetHomeAsync()
    {
        return Ok(await _movieCatalogService.GetHomeAsync());
    }

    // Ficha completa de um filme
    [HttpGet("api/movies/{movieId:guid}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] Guid movieId)
    {
        try
        {
            return Ok(await _movieCatalogService.GetDetailAsync(movieId));
        }
        catch (MovieNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

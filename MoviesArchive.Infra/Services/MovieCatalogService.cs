using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Infra.Services;

public class MovieCatalogService : IMovieCatalogService
{
    private const int ShelfSize = 10;

    private readonly IMovieRepository _movieRepository;

    public MovieCatalogService(
        IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<PagedResponseDto<MovieCardDto>> SearchAsync(MovieSearchFilter filter)
    {
        var (items, totalCount) = await _movieRepository.SearchAsync(filter);

        return new PagedResponseDto<MovieCardDto>
        {
            Items = items.Select(ToCard).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)filter.PageSize)),
        };
    }

    public async Task<MovieDetailDto> GetDetailAsync(Guid movieId)
    {
        var movie = await _movieRepository.GetByIdAsync(movieId)
            ?? throw new MovieNotFoundException();

        var votes = await _movieRepository.CountReviewsAsync(movieId);
        return ToDetail(movie, votes);
    }

    public async Task<MovieHomeDto> GetHomeAsync()
    {
        var topRated = await _movieRepository.GetTopRatedAsync(ShelfSize);

        var shelves = new List<MovieShelfDto>
        {
            new()
            {
                Id = "mais-bem-avaliados",
                Title = "Mais bem avaliados",
                Movies = topRated.Select(ToCard).ToList(),
            },
        };

        // Uma prateleira por gênero que tiver filmes avaliados
        foreach (var genre in Enum.GetValues<Genre>())
        {
            var movies = await _movieRepository.GetTopRatedAsync(ShelfSize, genre.ToString());
            if (movies.Count == 0) continue;

            shelves.Add(new MovieShelfDto
            {
                Id = $"genero-{genre.ToString().ToLowerInvariant()}",
                Title = genre.ToDisplayName(),
                Movies = movies.Select(ToCard).ToList(),
            });
        }

        MovieDetailDto? featured = null;
        if (topRated.Count > 0)
        {
            var votes = await _movieRepository.CountReviewsAsync(topRated[0].Id);
            featured = ToDetail(topRated[0], votes);
        }

        return new MovieHomeDto
        {
            Featured = featured,
            Shelves = shelves,
        };
    }

    // Também usado pela watchlist
    internal static MovieCardDto ToCard(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        ReleaseYear = movie.ReleaseDate?.Year,
        Language = movie.Language,
        Director = movie.Director,
        Genres = GenreNames(movie),
        Rating = movie.Rating,
        PosterUrl = movie.PosterUrl,
    };

    private static MovieDetailDto ToDetail(Movie movie, int votes) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        ReleaseYear = movie.ReleaseDate?.Year,
        Language = movie.Language,
        Director = movie.Director,
        Genres = GenreNames(movie),
        Rating = movie.Rating,
        Votes = votes,
        Synopsis = movie.Description,
        PosterUrl = movie.PosterUrl,
    };

    // Converte os códigos gravados no banco para o nome exibido, ignorando valores desconhecidos
    private static List<string> GenreNames(Movie movie) =>
        movie.Genres
            .Select(code => Enum.TryParse<Genre>(code, out var genre) ? genre.ToDisplayName() : null)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name)
            .ToList();
}

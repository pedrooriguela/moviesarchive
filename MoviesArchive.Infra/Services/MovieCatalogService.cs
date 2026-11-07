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
            Items = await ToCardsAsync(items),
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

        return await ToDetailAsync(movie);
    }

    public async Task<MovieHomeDto> GetHomeAsync()
    {
        // Só entram filmes que já têm avaliações
        var topRated = await _movieRepository.GetTopRatedAsync(ShelfSize);

        var shelves = new List<MovieShelfDto>();
        if (topRated.Count > 0)
        {
            shelves.Add(new MovieShelfDto
            {
                Id = "mais-bem-avaliados",
                Title = "Mais bem avaliados",
                Movies = await ToCardsAsync(topRated),
            });
        }

        // Uma prateleira por gênero: primeiro os avaliados (pela média), depois os ainda sem avaliação
        foreach (var genre in Enum.GetValues<Genre>())
        {
            var movies = await _movieRepository.GetTopRatedAsync(ShelfSize, genre.ToString(), onlyReviewed: false);
            if (movies.Count == 0) continue;

            shelves.Add(new MovieShelfDto
            {
                Id = $"genero-{genre.ToString().ToLowerInvariant()}",
                Title = genre.ToDisplayName(),
                Movies = await ToCardsAsync(movies),
            });
        }

        MovieDetailDto? featured = null;
        if (topRated.Count > 0)
            featured = await ToDetailAsync(topRated[0]);

        return new MovieHomeDto
        {
            Featured = featured,
            Shelves = shelves,
        };
    }

    // Cartões com a média das avaliações (tabela Reviews); sem avaliações, Rating fica null
    internal async Task<List<MovieCardDto>> ToCardsAsync(IReadOnlyCollection<Movie> movies)
    {
        var stats = await _movieRepository.GetRatingStatsAsync(movies.Select(movie => movie.Id).ToList());
        return movies.Select(movie => ToCard(movie, stats.GetValueOrDefault(movie.Id))).ToList();
    }

    private async Task<MovieDetailDto> ToDetailAsync(Movie movie)
    {
        var stats = await _movieRepository.GetRatingStatsAsync([movie.Id]);
        return ToDetail(movie, stats.GetValueOrDefault(movie.Id));
    }

    private static MovieCardDto ToCard(Movie movie, MovieRatingStats? stats) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        ReleaseYear = movie.ReleaseDate?.Year,
        Language = movie.Language,
        Director = movie.Director,
        Genres = GenreNames(movie),
        Rating = stats?.AverageRating,
        PosterUrl = movie.PosterUrl,
        AgeRating = movie.AgeRating,
    };

    private static MovieDetailDto ToDetail(Movie movie, MovieRatingStats? stats) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        ReleaseYear = movie.ReleaseDate?.Year,
        Language = movie.Language,
        Director = movie.Director,
        Genres = GenreNames(movie),
        Rating = stats?.AverageRating,
        Votes = stats?.ReviewCount ?? 0,
        Synopsis = movie.Description,
        PosterUrl = movie.PosterUrl,
        AgeRating = movie.AgeRating,
        RuntimeMinutes = movie.RuntimeMinutes,
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

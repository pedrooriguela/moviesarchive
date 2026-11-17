using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Movies.Dtos;
using MoviesArchive.Domain.Models.Recommendations;

namespace MoviesArchive.Infra.Services;

public class MovieCatalogService : IMovieCatalogService
{
    private const int ShelfSize = 10;

    private readonly IMovieRepository _movieRepository;
    private readonly ViewerContext _viewer;

    public MovieCatalogService(
        IMovieRepository movieRepository,
        ViewerContext viewer)
    {
        _movieRepository = movieRepository;
        _viewer = viewer;
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
        var shelves = new List<MovieShelfDto>();

        // Primeira prateleira: recomendações para quem está logado (só aparece se houver histórico)
        var recommended = await GetRecommendationsAsync();
        if (recommended.Count > 0)
        {
            shelves.Add(new MovieShelfDto
            {
                Id = "recomendados",
                Title = "Recomendado para você",
                Movies = recommended,
            });
        }

        // Só entram filmes que já têm avaliações
        var topRated = await _movieRepository.GetTopRatedAsync(ShelfSize);
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

    // Cartões com a média das avaliações (tabela Reviews; sem avaliações, Rating fica null)
    // e com a marca de "visto" para quem está logado
    public async Task<List<MovieCardDto>> ToCardsAsync(IReadOnlyCollection<Movie> movies)
    {
        var ids = movies.Select(movie => movie.Id).ToList();
        var stats = await _movieRepository.GetRatingStatsAsync(ids);
        var watched = await GetWatchedAsync(ids);
        return movies.Select(movie => ToCard(movie, stats.GetValueOrDefault(movie.Id), watched.Contains(movie.Id))).ToList();
    }

    // O cálculo está em RecommendationEngine (e explicado em docs/recomendacoes.md);
    // aqui só se junta o que ele precisa
    private async Task<List<MovieCardDto>> GetRecommendationsAsync()
    {
        if (_viewer.UserId is not Guid userId)
            return [];

        var signals = await _movieRepository.GetViewerSignalsAsync(userId);
        if (signals.IsEmpty)
            return [];

        // O filtro de classificação indicativa já vale aqui: menores só recebem filmes da idade deles
        var catalog = await _movieRepository.GetAllAsync();
        var community = await _movieRepository.GetRatingStatsAsync(catalog.Select(movie => movie.Id).ToList());

        var recommendations = RecommendationEngine.Recommend(catalog, signals, community, ShelfSize);
        if (recommendations.Count == 0)
            return [];

        var cards = await ToCardsAsync(recommendations.Select(item => item.Movie).ToList());
        for (var index = 0; index < cards.Count; index++)
            cards[index].Reason = recommendations[index].Reason;

        return cards;
    }

    // "Visto" = a pessoa deu play pelo menos uma vez (tem entrada no WatchHistory)
    private async Task<HashSet<Guid>> GetWatchedAsync(IReadOnlyCollection<Guid> movieIds) =>
        _viewer.UserId is Guid userId
            ? await _movieRepository.GetWatchedMovieIdsAsync(userId, movieIds)
            : [];

    private async Task<MovieDetailDto> ToDetailAsync(Movie movie)
    {
        var stats = await _movieRepository.GetRatingStatsAsync([movie.Id]);
        var watched = await GetWatchedAsync([movie.Id]);
        return ToDetail(movie, stats.GetValueOrDefault(movie.Id), watched.Contains(movie.Id));
    }

    private static MovieCardDto ToCard(Movie movie, MovieRatingStats? stats, bool watched) => new()
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
        Watched = watched,
    };

    private static MovieDetailDto ToDetail(Movie movie, MovieRatingStats? stats, bool watched) => new()
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
        Watched = watched,
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

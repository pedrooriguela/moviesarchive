using MoviesArchive.Infra.Database;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace MoviesArchive.Infra.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public MovieRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Movie>> SearchByTitleAsync(string? search, int limit)
    {
        // Seletor de filmes das listas: só o título, com as mesmas regras de acento e palavras da busca
        var query = ApplyTextSearch(_dbContext.Movies.AsNoTracking(), search, includeDirector: false);

        return await query
            .OrderBy(movie => movie.Title)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(List<Movie> Items, int TotalCount)> SearchAsync(MovieSearchFilter filter)
    {
        // Texto: título e diretor. Gênero não entra aqui, porque tem o filtro próprio logo abaixo.
        var query = ApplyTextSearch(_dbContext.Movies.AsNoTracking(), filter.Query, includeDirector: true);

        if (!string.IsNullOrWhiteSpace(filter.Language))
        {
            var language = filter.Language;
            query = query.Where(movie => movie.Language == language);
        }

        if (filter.YearFrom is int yearFrom)
        {
            var start = new DateOnly(yearFrom, 1, 1);
            query = query.Where(movie => movie.ReleaseDate >= start);
        }

        if (filter.YearTo is int yearTo)
        {
            var end = new DateOnly(yearTo, 12, 31);
            query = query.Where(movie => movie.ReleaseDate <= end);
        }

        // Com gêneros marcados, entra o filme que tiver pelo menos um deles
        // (vira o operador de sobreposição de arrays do PostgreSQL, &&)
        if (filter.Genres.Count > 0)
        {
            var genres = filter.Genres.ToList();
            query = query.Where(movie => movie.Genres.Any(genre => genres.Contains(genre)));
        }

        var totalCount = await query.CountAsync();

        var ordered = ApplySort(query, filter.Sort);

        var items = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // Ordenação da busca. Pela avaliação, usa a média da tabela Reviews (a mesma exibida nos
    // cartões) e deixa os filmes ainda sem avaliação por último, nos dois sentidos.
    private IQueryable<Movie> ApplySort(IQueryable<Movie> query, string sort)
    {
        switch (sort)
        {
            case "year-desc":
                return query.OrderByDescending(movie => movie.ReleaseDate).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id);
            case "year-asc":
                return query.OrderBy(movie => movie.ReleaseDate).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id);
            case "rating-desc":
            case "rating-asc":
            {
                var ranked = query.Select(movie => new
                {
                    Movie = movie,
                    Average = _dbContext.Reviews
                        .Where(review => review.MovieId == movie.Id)
                        .Average(review => (decimal?)review.Rating),
                });

                var unratedLast = ranked.OrderBy(row => row.Average == null);
                var byAverage = sort == "rating-desc"
                    ? unratedLast.ThenByDescending(row => row.Average)
                    : unratedLast.ThenBy(row => row.Average);

                return byAverage
                    .ThenBy(row => row.Movie.Title)
                    .ThenBy(row => row.Movie.Id)
                    .Select(row => row.Movie);
            }
            default:
                return query.OrderBy(movie => movie.Title).ThenBy(movie => movie.Id);
        }
    }

    // Sujeito ao filtro de classificação indicativa, como todas as consultas de filmes
    public Task<List<Movie>> GetAllAsync() =>
        _dbContext.Movies
            .AsNoTracking()
            .OrderBy(movie => movie.Title)
            .ToListAsync();

    public async Task<ViewerSignals> GetViewerSignalsAsync(Guid userId)
    {
        // Quantas vezes deu play em cada filme
        var watchCounts = await _dbContext.WatchHistory
            .AsNoTracking()
            .Where(entry => entry.UserId == userId)
            .GroupBy(entry => entry.MovieId)
            .Select(group => new { MovieId = group.Key, Plays = group.Count() })
            .ToDictionaryAsync(row => row.MovieId, row => row.Plays);

        // Uma avaliação por filme (índice único em Reviews)
        var ratings = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review => review.UserId == userId)
            .ToDictionaryAsync(review => review.MovieId, review => review.Rating);

        // A watchlist é a lista fixa do usuário (Lists.IsWatchlist)
        var watchlist = await _dbContext.ListMovies
            .AsNoTracking()
            .Where(item => _dbContext.Lists.Any(list => list.Id == item.ListId && list.UserId == userId && list.IsWatchlist))
            .Select(item => item.MovieId)
            .ToListAsync();

        return new ViewerSignals(watchCounts, ratings, watchlist.ToHashSet());
    }

    public async Task<HashSet<Guid>> GetWatchedMovieIdsAsync(Guid userId, IReadOnlyCollection<Guid> movieIds)
    {
        if (movieIds.Count == 0)
            return [];

        var ids = movieIds.Distinct().ToList();
        var watched = await _dbContext.WatchHistory
            .AsNoTracking()
            .Where(entry => entry.UserId == userId && ids.Contains(entry.MovieId))
            .Select(entry => entry.MovieId)
            .Distinct()
            .ToListAsync();

        return watched.ToHashSet();
    }

    public async Task<Movie?> GetByIdAsync(Guid movieId)
    {
        return await _dbContext.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(movie => movie.Id == movieId);
    }

    public Task<int> CountReviewsAsync(Guid movieId) =>
        _dbContext.Reviews.CountAsync(review => review.MovieId == movieId);

    public async Task<Dictionary<Guid, MovieRatingStats>> GetRatingStatsAsync(IReadOnlyCollection<Guid> movieIds)
    {
        if (movieIds.Count == 0)
            return new Dictionary<Guid, MovieRatingStats>();

        var ids = movieIds.Distinct().ToList();
        var rows = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review => ids.Contains(review.MovieId))
            .GroupBy(review => review.MovieId)
            .Select(group => new
            {
                MovieId = group.Key,
                Average = group.Average(review => review.Rating),
                Count = group.Count(),
            })
            .ToListAsync();

        return rows.ToDictionary(
            row => row.MovieId,
            row => new MovieRatingStats(Math.Round(row.Average, 2), row.Count));
    }

    // Ordena pela média das avaliações (tabela Reviews). Com onlyReviewed = false, os filmes
    // ainda sem avaliação também entram, depois dos avaliados, em ordem de título.
    public async Task<List<Movie>> GetTopRatedAsync(int limit, string? genre = null, bool onlyReviewed = true)
    {
        var query = _dbContext.Movies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(movie => movie.Genres.Contains(genre));
        }

        var ranked = query.Select(movie => new
        {
            Movie = movie,
            Average = _dbContext.Reviews
                .Where(review => review.MovieId == movie.Id)
                .Average(review => (decimal?)review.Rating),
        });

        if (onlyReviewed)
        {
            ranked = ranked.Where(row => row.Average != null);
        }

        return await ranked
            .OrderBy(row => row.Average == null)
            .ThenByDescending(row => row.Average)
            .ThenBy(row => row.Movie.Title)
            .Take(limit)
            .Select(row => row.Movie)
            .ToListAsync();
    }

    // ---- Painel do administrador. IgnoreQueryFilters: entram os filmes removidos e os de
    // qualquer classificação indicativa, porque é ali que eles são cadastrados e restaurados.

    public async Task<(List<Movie> Items, int TotalCount)> SearchForAdminAsync(
        string? query, AdminMovieStatus status, int page, int pageSize)
    {
        var movies = ApplyTextSearch(_dbContext.Movies.IgnoreQueryFilters().AsNoTracking(), query, includeDirector: true);

        movies = status switch
        {
            AdminMovieStatus.Active => movies.Where(movie => movie.DeletedAt == null),
            AdminMovieStatus.Removed => movies.Where(movie => movie.DeletedAt != null),
            _ => movies,
        };

        var totalCount = await movies.CountAsync();

        var items = await movies
            .OrderBy(movie => movie.Title)
            .ThenBy(movie => movie.ReleaseDate)
            .ThenBy(movie => movie.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // Rastreado pelo EF: as alterações são gravadas com SaveChangesAsync
    public Task<Movie?> GetForAdminAsync(Guid movieId) =>
        _dbContext.Movies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(movie => movie.Id == movieId);

    // Mesmo título (sem diferenciar maiúsculas) e mesmo ano, entre os filmes que estão no catálogo
    public Task<bool> ExistsInCatalogAsync(string title, int? releaseYear, Guid? ignoredMovieId = null)
    {
        var normalized = title.Trim().ToLower();
        return _dbContext.Movies
            .IgnoreQueryFilters()
            .Where(movie => movie.DeletedAt == null)
            .Where(movie => ignoredMovieId == null || movie.Id != ignoredMovieId)
            .Where(movie => movie.Title.ToLower() == normalized)
            .AnyAsync(movie => releaseYear == null
                ? movie.ReleaseDate == null
                : movie.ReleaseDate != null && movie.ReleaseDate.Value.Year == releaseYear);
    }

    // Cliques no play de cada filme, desde sempre (filmes sem acesso ficam fora do dicionário)
    public async Task<Dictionary<Guid, int>> CountAccessesAsync(IReadOnlyCollection<Guid> movieIds)
    {
        if (movieIds.Count == 0)
            return new Dictionary<Guid, int>();

        var ids = movieIds.Distinct().ToList();
        return await _dbContext.WatchHistory
            .IgnoreQueryFilters()
            .Where(entry => ids.Contains(entry.MovieId))
            .GroupBy(entry => entry.MovieId)
            .Select(group => new { MovieId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.MovieId, row => row.Count);
    }

    public async Task AddAsync(Movie movie)
    {
        await _dbContext.Movies.AddAsync(movie);
        await _dbContext.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    // Cada palavra da busca precisa aparecer no título (ou no diretor), ignorando acentos e maiúsculas:
    // "kubrick iluminado" acha O Iluminado, "toquio" acha Viagem a Tóquio
    private static IQueryable<Movie> ApplyTextSearch(IQueryable<Movie> query, string? text, bool includeDirector)
    {
        foreach (var term in SearchText.Terms(text))
        {
            var pattern = ToLikePattern(term);
            query = includeDirector
                ? query.Where(movie =>
                    EF.Functions.Like(SearchText.Translate(movie.Title.ToLower(), SearchText.Accented, SearchText.Plain), pattern) ||
                    (movie.Director != null &&
                     EF.Functions.Like(SearchText.Translate(movie.Director.ToLower(), SearchText.Accented, SearchText.Plain), pattern)))
                : query.Where(movie =>
                    EF.Functions.Like(SearchText.Translate(movie.Title.ToLower(), SearchText.Accented, SearchText.Plain), pattern));
        }

        return query;
    }

    // Escapa os curingas do LIKE para buscar o texto literalmente
    private static string ToLikePattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escaped}%";
    }
}

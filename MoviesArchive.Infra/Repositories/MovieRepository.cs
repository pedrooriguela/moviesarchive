using System.Globalization;
using System.Text;
using MoviesArchive.Infra.Database;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
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
        var query = _dbContext.Movies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = ToLikePattern(search);
            query = query.Where(movie => EF.Functions.ILike(movie.Title, pattern));
        }

        return await query
            .OrderBy(movie => movie.Title)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(List<Movie> Items, int TotalCount)> SearchAsync(MovieSearchFilter filter)
    {
        var query = _dbContext.Movies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var pattern = ToLikePattern(filter.Query);
            // Gêneros são buscados pelo nome exibido (ex.: "ficcao" acha "Ficção científica")
            var genreMovieIds = await GetMovieIdsByGenresAsync(MatchingGenreCodes(filter.Query));

            query = query.Where(movie =>
                EF.Functions.ILike(movie.Title, pattern) ||
                (movie.Director != null && EF.Functions.ILike(movie.Director, pattern)) ||
                genreMovieIds.Contains(movie.Id));
        }

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

        var totalCount = await query.CountAsync();

        var ordered = filter.Sort switch
        {
            "year-desc" => query.OrderByDescending(movie => movie.ReleaseDate).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id),
            "year-asc" => query.OrderBy(movie => movie.ReleaseDate).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id),
            _ => query.OrderBy(movie => movie.Title).ThenBy(movie => movie.Id),
        };

        var items = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Movie?> GetByIdAsync(Guid movieId)
    {
        return await _dbContext.Movies
            .AsNoTracking()
            .FirstOrDefaultAsync(movie => movie.Id == movieId);
    }

    public Task<int> CountReviewsAsync(Guid movieId) =>
        _dbContext.Reviews.CountAsync(review => review.MovieId == movieId);

    public async Task<List<Movie>> GetTopRatedAsync(int limit, string? genre = null)
    {
        var query = _dbContext.Movies.AsNoTracking().Where(movie => movie.Rating != null);

        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(movie => movie.Genres.Contains(genre));
        }

        return await query
            .OrderByDescending(movie => movie.Rating)
            .ThenBy(movie => movie.Title)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<List<Guid>> GetMovieIdsByGenresAsync(IEnumerable<string> genreCodes)
    {
        var ids = new List<Guid>();
        foreach (var code in genreCodes)
        {
            ids.AddRange(await _dbContext.Movies
                .AsNoTracking()
                .Where(movie => movie.Genres.Contains(code))
                .Select(movie => movie.Id)
                .ToListAsync());
        }

        return ids.Distinct().ToList();
    }

    // Códigos dos gêneros cujo nome exibido contém o texto buscado
    private static List<string> MatchingGenreCodes(string query)
    {
        var term = Normalize(query);
        return Enum.GetValues<Genre>()
            .Where(genre => Normalize(genre.ToDisplayName()).Contains(term))
            .Select(genre => genre.ToString())
            .ToList();
    }

    // Minúsculas e sem acentos, para comparar textos
    private static string Normalize(string text)
    {
        var withoutMarks = text.Trim().Normalize(NormalizationForm.FormD)
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);

        return new string(withoutMarks.ToArray()).ToLowerInvariant();
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

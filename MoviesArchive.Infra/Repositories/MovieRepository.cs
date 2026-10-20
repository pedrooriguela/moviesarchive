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
            // Escapa os curingas do LIKE para buscar o texto literalmente
            var pattern = search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            query = query.Where(movie => EF.Functions.ILike(movie.Title, $"%{pattern}%"));
        }

        return await query
            .OrderBy(movie => movie.Title)
            .Take(limit)
            .ToListAsync();
    }
}

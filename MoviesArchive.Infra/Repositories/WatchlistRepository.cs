using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Watchlist;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class WatchlistRepository : IWatchlistRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public WatchlistRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ContainsAsync(Guid userId, Guid movieId) =>
        _dbContext.Watchlist.AnyAsync(item => item.UserId == userId && item.MovieId == movieId);

    public Task<WatchlistItem?> GetAsync(Guid userId, Guid movieId) =>
        _dbContext.Watchlist.FirstOrDefaultAsync(item => item.UserId == userId && item.MovieId == movieId);

    // Filmes da watchlist, do adicionado por último ao mais antigo
    public Task<List<Movie>> GetMoviesAsync(Guid userId) =>
        _dbContext.Watchlist
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Join(
                _dbContext.Movies.AsNoTracking(),
                item => item.MovieId,
                movie => movie.Id,
                (item, movie) => new { item.AddedAt, Movie = movie })
            .OrderByDescending(row => row.AddedAt)
            .Select(row => row.Movie)
            .ToListAsync();

    public async Task AddAsync(WatchlistItem item)
    {
        await _dbContext.Watchlist.AddAsync(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(WatchlistItem item)
    {
        _dbContext.Watchlist.Remove(item);
        await _dbContext.SaveChangesAsync();
    }
}

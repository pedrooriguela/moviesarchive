using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Watchlist;

namespace MoviesArchive.Domain.Interfaces;

public interface IWatchlistRepository
{
    Task<bool> ContainsAsync(Guid userId, Guid movieId);
    Task<WatchlistItem?> GetAsync(Guid userId, Guid movieId);
    Task<List<Movie>> GetMoviesAsync(Guid userId);
    Task AddAsync(WatchlistItem item);
    Task RemoveAsync(WatchlistItem item);
}

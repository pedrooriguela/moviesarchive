using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Interfaces;

// A watchlist é a lista do usuário com IsWatchlist = true (tabelas Lists e ListMovies)
public interface IWatchlistRepository
{
    Task<bool> ContainsAsync(Guid userId, Guid movieId);
    Task<List<Movie>> GetMoviesAsync(Guid userId);
}

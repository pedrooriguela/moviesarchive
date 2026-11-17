using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

// Consultas da watchlist, que agora é a lista fixa do usuário (Lists.IsWatchlist).
// Adicionar e remover filmes passa pelas mesmas regras das outras listas (MovieListService).
public class WatchlistRepository : IWatchlistRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public WatchlistRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ContainsAsync(Guid userId, Guid movieId) =>
        _dbContext.ListMovies.AnyAsync(item =>
            item.MovieId == movieId &&
            _dbContext.Lists.Any(list => list.Id == item.ListId && list.UserId == userId && list.IsWatchlist));

    // Filmes da watchlist na ordem da lista
    public Task<List<Movie>> GetMoviesAsync(Guid userId) =>
        _dbContext.ListMovies
            .AsNoTracking()
            .Where(item => _dbContext.Lists.Any(list => list.Id == item.ListId && list.UserId == userId && list.IsWatchlist))
            .OrderBy(item => item.Position)
            .ThenBy(item => item.AddedAt)
            .Select(item => item.Movie!)
            .ToListAsync();
}

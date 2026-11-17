using MoviesArchive.Domain.Models.Lists;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieListRepository
{
    Task AddAsync(MovieList list);
    Task UpdateAsync(MovieList list);
    Task DeleteAsync(MovieList list);
    Task<MovieList?> GetByIdAsync(Guid id);
    Task<MovieList?> GetVisibleByIdAsync(Guid id, Guid requesterId);
    Task<List<MovieList>> GetVisibleByUserIdAsync(Guid ownerId, Guid requesterId);

    // A watchlist do usuário, criada na hora se ainda não existir
    Task<MovieList> GetOrCreateWatchlistAsync(Guid userId);
    Task<bool> TitleExistsAsync(Guid userId, string title, Guid? ignoredListId = null);
    Task<bool> MovieExistsAsync(Guid movieId);
    Task AddItemAsync(ListMovie item);
    Task RemoveItemAsync(ListMovie item);
}

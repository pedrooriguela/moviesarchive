using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IWatchlistService
{
    Task<List<MovieCardDto>> GetMoviesAsync(Guid userId);
    Task AddAsync(Guid userId, Guid movieId);
    Task RemoveAsync(Guid userId, Guid movieId);
}

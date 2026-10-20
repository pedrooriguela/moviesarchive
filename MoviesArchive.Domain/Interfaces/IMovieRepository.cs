using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieRepository
{
    Task<List<Movie>> SearchByTitleAsync(string? search, int limit);
}

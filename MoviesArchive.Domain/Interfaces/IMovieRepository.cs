using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieRepository
{
    Task<List<Movie>> SearchByTitleAsync(string? search, int limit);
    Task<(List<Movie> Items, int TotalCount)> SearchAsync(MovieSearchFilter filter);
    Task<Movie?> GetByIdAsync(Guid movieId);
    Task<int> CountReviewsAsync(Guid movieId);
    Task<List<Movie>> GetTopRatedAsync(int limit, string? genre = null);
}

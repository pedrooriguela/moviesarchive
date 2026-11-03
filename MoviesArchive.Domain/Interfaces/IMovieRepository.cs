using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Interfaces;

public interface IMovieRepository
{
    Task<List<Movie>> SearchByTitleAsync(string? search, int limit);
    Task<(List<Movie> Items, int TotalCount)> SearchAsync(MovieSearchFilter filter);
    Task<Movie?> GetByIdAsync(Guid movieId);
    Task<int> CountReviewsAsync(Guid movieId);

    // Só traz os filmes que têm avaliações; os que não têm ficam fora do dicionário
    Task<Dictionary<Guid, MovieRatingStats>> GetRatingStatsAsync(IReadOnlyCollection<Guid> movieIds);
    Task<List<Movie>> GetTopRatedAsync(int limit, string? genre = null, bool onlyReviewed = true);
}

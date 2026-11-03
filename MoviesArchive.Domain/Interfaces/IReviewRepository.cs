using MoviesArchive.Domain.Models.Reviews;

namespace MoviesArchive.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByUserAndMovieAsync(Guid userId, Guid movieId);
    Task<(List<ReviewWithAuthor> Items, int TotalCount)> GetByMovieAsync(Guid movieId, int page, int pageSize);
    Task<Dictionary<decimal, int>> CountByRatingAsync(Guid movieId);
    Task AddAsync(Review review);
    Task UpdateAsync(Review review);
    Task DeleteAsync(Review review);
}

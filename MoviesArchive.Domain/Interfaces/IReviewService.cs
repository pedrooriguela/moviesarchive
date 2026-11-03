using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IReviewService
{
    Task<MovieReviewsResponseDto> GetMovieReviewsAsync(Guid movieId, int page, int pageSize);
    Task<MovieUserStateDto> GetUserStateAsync(Guid movieId, Guid userId);
    Task<ReviewResponseDto> UpsertAsync(Guid movieId, Guid userId, UpsertReviewRequestDto request);
    Task DeleteAsync(Guid movieId, Guid userId);
}

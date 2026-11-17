using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IReviewService
{
    // viewerId: quem está vendo, para marcar o que já curtiu
    Task<MovieReviewsResponseDto> GetMovieReviewsAsync(Guid movieId, int page, int pageSize, Guid? viewerId);
    Task<MovieUserStateDto> GetUserStateAsync(Guid movieId, Guid userId);
    Task<ReviewResponseDto> UpsertAsync(Guid movieId, Guid userId, UpsertReviewRequestDto request);
    Task DeleteAsync(Guid movieId, Guid userId);
}

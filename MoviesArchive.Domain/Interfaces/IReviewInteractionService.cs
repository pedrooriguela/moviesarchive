using MoviesArchive.Domain.Models.Reviews.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IReviewInteractionService
{
    Task<List<ReplyResponseDto>> GetRepliesAsync(Guid reviewId, Guid? viewerId);
    Task<ReplyResponseDto> AddReplyAsync(Guid reviewId, Guid userId, CreateReplyRequestDto request);
    Task DeleteReplyAsync(Guid replyId, Guid userId);
    Task<LikeStateDto> SetReviewLikeAsync(Guid reviewId, Guid userId, bool liked);
    Task<LikeStateDto> SetReplyLikeAsync(Guid replyId, Guid userId, bool liked);
}

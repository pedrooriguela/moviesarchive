using MoviesArchive.Domain.Models.Reviews;

namespace MoviesArchive.Domain.Interfaces;

// Respostas às avaliações e curtidas (em avaliações e em respostas)
public interface IReviewInteractionRepository
{
    Task<Review?> GetReviewAsync(Guid reviewId);
    Task<ReviewReply?> GetReplyAsync(Guid replyId);

    // Todas as respostas da avaliação, das mais antigas às mais novas
    Task<List<ReplyWithAuthor>> GetRepliesAsync(Guid reviewId, Guid? viewerId);
    Task<ReplyWithAuthor?> GetReplyWithAuthorAsync(Guid replyId, Guid? viewerId);
    Task<bool> HasChildRepliesAsync(Guid replyId);
    Task AddReplyAsync(ReviewReply reply);
    Task UpdateReplyAsync(ReviewReply reply);
    Task DeleteReplyAsync(ReviewReply reply);

    // Curtidas e quantidade de respostas de várias avaliações de uma vez
    Task<Dictionary<Guid, ReviewEngagement>> GetReviewEngagementAsync(IReadOnlyCollection<Guid> reviewIds, Guid? viewerId);

    // Idempotentes: curtir de novo ou descurtir o que não foi curtido não dá erro.
    // Devolvem true quando algo mudou (para avisar o autor só na curtida nova).
    Task<bool> SetReviewLikeAsync(Guid userId, Guid reviewId, bool liked);
    Task<int> CountReviewLikesAsync(Guid reviewId);
    Task<bool> SetReplyLikeAsync(Guid userId, Guid replyId, bool liked);
    Task<int> CountReplyLikesAsync(Guid replyId);
}

namespace MoviesArchive.Domain.Models.Reviews;

// Curtida em uma resposta (tabela "ResponseLikes"). Chave composta (UserId, ReplyId).
public class ReviewReplyLike
{
    public ReviewReplyLike(Guid userId, Guid replyId)
    {
        UserId = userId;
        ReplyId = replyId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid ReplyId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

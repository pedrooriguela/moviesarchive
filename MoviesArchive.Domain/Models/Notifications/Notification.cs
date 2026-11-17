using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Notifications;

// Aviso para UserId de que ActorId curtiu ou respondeu algo dele (tabela "Notifications").
// ReplyId é a resposta curtida, ou a resposta nova (quando é aviso de resposta).
public class Notification : Entity
{
    public Notification(Guid userId, Guid actorId, NotificationType type, Guid movieId, Guid reviewId, Guid? replyId)
    {
        UserId = userId;
        ActorId = actorId;
        Type = type;
        MovieId = movieId;
        ReviewId = reviewId;
        ReplyId = replyId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid ActorId { get; set; }
    public NotificationType Type { get; set; }
    public Guid MovieId { get; set; }
    public Guid ReviewId { get; set; }
    public Guid? ReplyId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public class Builder
    {
        public static Notification Create(Guid userId, Guid actorId, NotificationType type, Guid movieId, Guid reviewId, Guid? replyId) =>
            new(userId, actorId, type, movieId, reviewId, replyId);
    }
}

using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Notifications;

// Aviso para UserId de algo que ActorId fez (tabela "Notifications"). Dois grupos:
// - avaliação: curtiu ou respondeu algo do usuário; MovieId e ReviewId preenchidos e ReplyId é a
//   resposta curtida, ou a resposta nova (quando é aviso de resposta)
// - plano familiar: convidou o usuário, ou aceitou/recusou o convite dele; só FamilyInviteId preenchido
public class Notification : Entity
{
    public Notification(Guid userId, Guid actorId, NotificationType type, Guid? movieId, Guid? reviewId, Guid? replyId, Guid? familyInviteId = null)
    {
        UserId = userId;
        ActorId = actorId;
        Type = type;
        MovieId = movieId;
        ReviewId = reviewId;
        ReplyId = replyId;
        FamilyInviteId = familyInviteId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid ActorId { get; set; }
    public NotificationType Type { get; set; }
    public Guid? MovieId { get; set; }
    public Guid? ReviewId { get; set; }
    public Guid? ReplyId { get; set; }
    public Guid? FamilyInviteId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    public bool IsAboutPlan => Type.IsAboutPlan();

    public class Builder
    {
        public static Notification Create(Guid userId, Guid actorId, NotificationType type, Guid movieId, Guid reviewId, Guid? replyId) =>
            new(userId, actorId, type, movieId, reviewId, replyId);

        public static Notification CreateForInvite(Guid userId, Guid actorId, NotificationType type, Guid familyInviteId)
        {
            if (!type.IsAboutPlan())
                throw new ArgumentException("Tipo de aviso não é de plano.", nameof(type));

            return new(userId, actorId, type, null, null, null, familyInviteId);
        }
    }
}

using MoviesArchive.Domain.Models.Notifications;

namespace MoviesArchive.Domain.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);

    // Descurtiu: o aviso daquela curtida some
    Task DeleteLikeAsync(Guid actorId, NotificationType type, Guid reviewId, Guid? replyId);

    // Resposta excluída: somem os avisos sobre ela (curtidas e o aviso de que ela foi escrita)
    Task DeleteForReplyAsync(Guid replyId);

    Task<(List<Notification> Items, int TotalCount)> GetPageAsync(Guid userId, int page, int pageSize);
    Task<int> CountUnreadAsync(Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId);
}

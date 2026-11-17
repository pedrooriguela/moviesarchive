using MoviesArchive.Domain.Models.Notifications.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface INotificationService
{
    Task<NotificationPageDto> GetMyAsync(Guid userId, int page, int pageSize);
    Task<int> CountUnreadAsync(Guid userId);
    Task MarkAllReadAsync(Guid userId);
    Task MarkReadAsync(Guid userId, Guid notificationId);
}

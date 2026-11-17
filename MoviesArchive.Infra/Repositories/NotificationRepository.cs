using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Notifications;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public NotificationRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notification notification)
    {
        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();
    }

    public Task DeleteLikeAsync(Guid actorId, NotificationType type, Guid reviewId, Guid? replyId) =>
        _dbContext.Notifications
            .Where(item => item.ActorId == actorId && item.Type == type &&
                item.ReviewId == reviewId && item.ReplyId == replyId)
            .ExecuteDeleteAsync();

    public Task DeleteForReplyAsync(Guid replyId) =>
        _dbContext.Notifications
            .Where(item => item.ReplyId == replyId)
            .ExecuteDeleteAsync();

    public async Task<(List<Notification> Items, int TotalCount)> GetPageAsync(Guid userId, int page, int pageSize)
    {
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<int> CountUnreadAsync(Guid userId) =>
        _dbContext.Notifications.CountAsync(item => item.UserId == userId && item.ReadAt == null);

    public Task MarkAllReadAsync(Guid userId) =>
        _dbContext.Notifications
            .Where(item => item.UserId == userId && item.ReadAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.ReadAt, (DateTimeOffset?)DateTimeOffset.UtcNow));

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId)
    {
        // Só as do próprio usuário: de outra pessoa, é como se não existisse
        var updated = await _dbContext.Notifications
            .Where(item => item.Id == notificationId && item.UserId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.ReadAt, (DateTimeOffset?)DateTimeOffset.UtcNow));
        return updated > 0;
    }
}

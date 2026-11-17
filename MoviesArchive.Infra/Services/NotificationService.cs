using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Notifications;
using MoviesArchive.Domain.Models.Notifications.Dtos;
using MoviesArchive.Domain.Models.Subscriptions;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Services;

public class NotificationService : INotificationService
{
    private const int ExcerptLength = 120;

    private readonly INotificationRepository _notificationRepository;
    private readonly MoviesArchiveDbContext _dbContext;

    public NotificationService(
        INotificationRepository notificationRepository,
        MoviesArchiveDbContext dbContext)
    {
        _notificationRepository = notificationRepository;
        _dbContext = dbContext;
    }

    public async Task<NotificationPageDto> GetMyAsync(Guid userId, int page, int pageSize)
    {
        var (items, totalCount) = await _notificationRepository.GetPageAsync(userId, page, pageSize);

        // Busca de uma vez quem fez, o filme e os textos citados nos avisos desta página
        var actorIds = items.Select(item => item.ActorId).Distinct().ToList();
        var movieIds = items.Where(item => item.MovieId != null).Select(item => item.MovieId!.Value).Distinct().ToList();
        var reviewIds = items.Where(item => item.ReviewId != null).Select(item => item.ReviewId!.Value).Distinct().ToList();
        var inviteIds = items.Where(item => item.FamilyInviteId != null).Select(item => item.FamilyInviteId!.Value).Distinct().ToList();
        var replyIds = items.Where(item => item.ReplyId != null).Select(item => item.ReplyId!.Value).Distinct().ToList();

        var actors = await _dbContext.Users.AsNoTracking()
            .Where(user => actorIds.Contains(user.Id))
            .Select(user => new { user.Id, user.Name, HasPhoto = user.PhotoData != null })
            .ToDictionaryAsync(user => user.Id);
        // IgnoreQueryFilters: o título aparece mesmo se o filme tiver saído da faixa etária
        var movies = await _dbContext.Movies.AsNoTracking().IgnoreQueryFilters()
            .Where(movie => movieIds.Contains(movie.Id))
            .Select(movie => new { movie.Id, movie.Title })
            .ToDictionaryAsync(movie => movie.Id, movie => movie.Title);
        var reviewComments = await _dbContext.Reviews.AsNoTracking()
            .Where(review => reviewIds.Contains(review.Id))
            .Select(review => new { review.Id, review.Comment })
            .ToDictionaryAsync(review => review.Id, review => review.Comment);
        var replyComments = await _dbContext.ReviewReplies.AsNoTracking()
            .Where(reply => replyIds.Contains(reply.Id))
            .Select(reply => new { reply.Id, reply.Comment })
            .ToDictionaryAsync(reply => reply.Id, reply => reply.Comment);
        // Convites dos avisos de plano, com o nome do plano e se a assinatura ainda está ativa
        var invites = await (
                from invite in _dbContext.FamilyInvites.AsNoTracking()
                join subscription in _dbContext.Subscriptions.AsNoTracking() on invite.SubscriptionId equals subscription.Id
                join plan in _dbContext.Plans.AsNoTracking() on subscription.PlanId equals plan.Id
                where inviteIds.Contains(invite.Id)
                select new InviteInfo(invite.Id, invite.Status, invite.ExpiresAt, subscription.EndedAt != null, plan.Name))
            .ToDictionaryAsync(invite => invite.Id);
        var now = DateTimeOffset.UtcNow;

        return new NotificationPageDto
        {
            Items = items.Select(item =>
            {
                var actor = actors.GetValueOrDefault(item.ActorId);
                var text = item.Type == NotificationType.ReviewLiked
                    ? item.ReviewId is Guid reviewId ? reviewComments.GetValueOrDefault(reviewId) : null
                    : item.ReplyId is Guid replyId ? replyComments.GetValueOrDefault(replyId) : null;
                var invite = item.FamilyInviteId is Guid inviteId ? invites.GetValueOrDefault(inviteId) : null;

                return new NotificationResponseDto
                {
                    Id = item.Id,
                    Type = item.Type,
                    ActorId = item.ActorId,
                    ActorName = actor?.Name ?? string.Empty,
                    // Mesma rota usada pelo UserController
                    ActorPhotoUrl = actor?.HasPhoto == true ? $"/api/users/{item.ActorId}/photo" : null,
                    MovieId = item.MovieId,
                    MovieTitle = item.MovieId is Guid movieId ? movies.GetValueOrDefault(movieId) ?? string.Empty : string.Empty,
                    ReviewId = item.ReviewId,
                    ReplyId = item.ReplyId,
                    Excerpt = Excerpt(text),
                    InviteId = item.FamilyInviteId,
                    PlanName = invite?.PlanName,
                    InviteStatus = item.FamilyInviteId == null ? null : InviteStatusOf(invite, now),
                    CreatedAt = item.CreatedAt,
                    IsRead = item.ReadAt != null,
                };
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
            UnreadCount = await _notificationRepository.CountUnreadAsync(userId),
        };
    }

    public Task<int> CountUnreadAsync(Guid userId) =>
        _notificationRepository.CountUnreadAsync(userId);

    public Task MarkAllReadAsync(Guid userId) =>
        _notificationRepository.MarkAllReadAsync(userId);

    public async Task MarkReadAsync(Guid userId, Guid notificationId)
    {
        if (!await _notificationRepository.MarkReadAsync(userId, notificationId))
            throw new NotificationNotFoundException();
    }

    // "Closed" junta tudo que não dá mais para responder: venceu, foi cancelado ou o plano acabou
    private static string InviteStatusOf(InviteInfo? invite, DateTimeOffset now) => invite switch
    {
        null => "Closed",
        { Status: InviteStatus.Accepted } => "Accepted",
        { Status: InviteStatus.Declined } => "Declined",
        { Status: InviteStatus.Pending, SubscriptionEnded: false } when invite.ExpiresAt > now => "Pending",
        _ => "Closed",
    };

    private sealed record InviteInfo(Guid Id, InviteStatus Status, DateTimeOffset ExpiresAt, bool SubscriptionEnded, string PlanName);

    private static string? Excerpt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var singleLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine.Length <= ExcerptLength ? singleLine : singleLine[..ExcerptLength].TrimEnd() + "…";
    }
}

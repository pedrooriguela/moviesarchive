using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class ReviewInteractionRepository : IReviewInteractionRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public ReviewInteractionRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Review?> GetReviewAsync(Guid reviewId) =>
        _dbContext.Reviews.FirstOrDefaultAsync(review => review.Id == reviewId);

    public Task<ReviewReply?> GetReplyAsync(Guid replyId) =>
        _dbContext.ReviewReplies.FirstOrDefaultAsync(reply => reply.Id == replyId);

    public async Task<List<ReplyWithAuthor>> GetRepliesAsync(Guid reviewId, Guid? viewerId)
    {
        var rows = await QueryWithAuthor(viewerId)
            .Where(row => row.Reply.ReviewId == reviewId)
            .OrderBy(row => row.Reply.CreatedAt)
            .ThenBy(row => row.Reply.Id)
            .ToListAsync();

        return rows.Select(ToReplyWithAuthor).ToList();
    }

    public async Task<ReplyWithAuthor?> GetReplyWithAuthorAsync(Guid replyId, Guid? viewerId)
    {
        var row = await QueryWithAuthor(viewerId)
            .FirstOrDefaultAsync(item => item.Reply.Id == replyId);

        return row == null ? null : ToReplyWithAuthor(row);
    }

    public Task<bool> HasChildRepliesAsync(Guid replyId) =>
        _dbContext.ReviewReplies.AnyAsync(reply => reply.ParentReplyId == replyId);

    public async Task AddReplyAsync(ReviewReply reply)
    {
        await _dbContext.ReviewReplies.AddAsync(reply);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateReplyAsync(ReviewReply reply)
    {
        if (_dbContext.Entry(reply).State == EntityState.Detached)
            _dbContext.ReviewReplies.Update(reply);

        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteReplyAsync(ReviewReply reply)
    {
        _dbContext.ReviewReplies.Remove(reply);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Dictionary<Guid, ReviewEngagement>> GetReviewEngagementAsync(IReadOnlyCollection<Guid> reviewIds, Guid? viewerId)
    {
        if (reviewIds.Count == 0)
            return new Dictionary<Guid, ReviewEngagement>();

        var ids = reviewIds.Distinct().ToList();
        var rows = await _dbContext.Reviews
            .AsNoTracking()
            .Where(review => ids.Contains(review.Id))
            .Select(review => new
            {
                review.Id,
                LikeCount = _dbContext.ReviewLikes.Count(like => like.ReviewId == review.Id),
                LikedByViewer = viewerId != null &&
                    _dbContext.ReviewLikes.Any(like => like.ReviewId == review.Id && like.UserId == viewerId),
                // Respostas excluídas que só seguram a conversa não contam
                ReplyCount = _dbContext.ReviewReplies.Count(reply => reply.ReviewId == review.Id && reply.DeletedAt == null),
            })
            .ToListAsync();

        return rows.ToDictionary(
            row => row.Id,
            row => new ReviewEngagement(row.LikeCount, row.LikedByViewer, row.ReplyCount));
    }

    public async Task<bool> SetReviewLikeAsync(Guid userId, Guid reviewId, bool liked)
    {
        var existing = await _dbContext.ReviewLikes
            .FirstOrDefaultAsync(like => like.UserId == userId && like.ReviewId == reviewId);

        if (liked && existing == null)
            await _dbContext.ReviewLikes.AddAsync(new ReviewLike(userId, reviewId));
        else if (!liked && existing != null)
            _dbContext.ReviewLikes.Remove(existing);
        else
            return false;

        return await SaveIgnoringDuplicateAsync();
    }

    public Task<int> CountReviewLikesAsync(Guid reviewId) =>
        _dbContext.ReviewLikes.CountAsync(like => like.ReviewId == reviewId);

    public async Task<bool> SetReplyLikeAsync(Guid userId, Guid replyId, bool liked)
    {
        var existing = await _dbContext.ReviewReplyLikes
            .FirstOrDefaultAsync(like => like.UserId == userId && like.ReplyId == replyId);

        if (liked && existing == null)
            await _dbContext.ReviewReplyLikes.AddAsync(new ReviewReplyLike(userId, replyId));
        else if (!liked && existing != null)
            _dbContext.ReviewReplyLikes.Remove(existing);
        else
            return false;

        return await SaveIgnoringDuplicateAsync();
    }

    public Task<int> CountReplyLikesAsync(Guid replyId) =>
        _dbContext.ReviewReplyLikes.CountAsync(like => like.ReplyId == replyId);

    // Dois cliques rápidos no "curtir" podem tentar inserir a mesma curtida duas vezes:
    // a chave primária barra a segunda, e o resultado final é o mesmo (e nada mudou)
    private async Task<bool> SaveIgnoringDuplicateAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    private IQueryable<ReplyRow> QueryWithAuthor(Guid? viewerId) =>
        _dbContext.ReviewReplies
            .AsNoTracking()
            .Join(
                _dbContext.Users.AsNoTracking(),
                reply => reply.UserId,
                user => user.Id,
                (reply, user) => new ReplyRow
                {
                    Reply = reply,
                    AuthorName = user.Name,
                    AuthorHasPhoto = user.PhotoData != null,
                    LikeCount = _dbContext.ReviewReplyLikes.Count(like => like.ReplyId == reply.Id),
                    LikedByViewer = viewerId != null &&
                        _dbContext.ReviewReplyLikes.Any(like => like.ReplyId == reply.Id && like.UserId == viewerId),
                });

    private static ReplyWithAuthor ToReplyWithAuthor(ReplyRow row) =>
        new(row.Reply, row.AuthorName, row.AuthorHasPhoto, row.LikeCount, row.LikedByViewer);

    private class ReplyRow
    {
        public ReviewReply Reply { get; set; } = null!;
        public string AuthorName { get; set; } = string.Empty;
        public bool AuthorHasPhoto { get; set; }
        public int LikeCount { get; set; }
        public bool LikedByViewer { get; set; }
    }
}

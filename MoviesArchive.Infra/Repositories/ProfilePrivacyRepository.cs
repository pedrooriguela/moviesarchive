using System.Data;
using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class ProfilePrivacyRepository(MoviesArchiveDbContext db) : IProfilePrivacyRepository
{
    private const int PageSize = 10;

    public async Task<bool> SetPrivacyAsync(Guid userId, bool isPrivate)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        // O mesmo bloqueio é usado em FollowAsync: mudar para público não deixa pedidos presos.
        var users = await db.Users.FromSqlInterpolated($"""
            SELECT * FROM "Users" WHERE "Id" = {userId} FOR UPDATE
            """).ToListAsync();
        var user = users.SingleOrDefault();
        if (user == null || user.Name == ReservedUserNames.Removed) return false;
        user.IsPrivate = isPrivate;
        await db.SaveChangesAsync();
        if (!isPrivate)
            await db.UserFollowers.Where(f => f.FollowingId == userId && !f.IsAccepted)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.IsAccepted, true));
        await tx.CommitAsync();
        return true;
    }

    public async Task<SharedProfileDto?> GetProfileAsync(Guid ownerId, Guid viewerId, int listsPage, int reviewsPage)
    {
        // As consultas usam a mesma fotografia de permissões e conteúdo.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        var profile = await db.Users.AsNoTracking().Where(u => u.Id == ownerId && u.Name != ReservedUserNames.Removed)
            .Select(u => new SharedProfileDto
            {
                Id = u.Id, Name = u.Name, IsPrivate = u.IsPrivate,
                PhotoUrl = u.PhotoData != null ? "/api/users/" + u.Id + "/photo" : null,
                IsFollowing = db.UserFollowers.Any(f => f.FollowerId == viewerId && f.FollowingId == ownerId && f.IsAccepted),
                IsRequested = db.UserFollowers.Any(f => f.FollowerId == viewerId && f.FollowingId == ownerId && !f.IsAccepted),
                CanViewContent = u.Id == viewerId || !u.IsPrivate || db.UserFollowers.Any(f =>
                    f.FollowerId == viewerId && f.FollowingId == ownerId && f.IsAccepted)
            }).SingleOrDefaultAsync();
        if (profile == null) return null;
        profile.ListsPage = listsPage;
        profile.ReviewsPage = reviewsPage;
        profile.ListsTotal = await db.Lists.CountAsync(l => l.UserId == ownerId && l.IsPublic);
        profile.ReviewsTotal = await db.Reviews.CountAsync(r => r.UserId == ownerId);
        profile.FollowersCount = await db.UserFollowers.CountAsync(f => f.FollowingId == ownerId && f.IsAccepted);
        profile.FollowingCount = await db.UserFollowers.CountAsync(f => f.FollowerId == ownerId && f.IsAccepted);
        if (profile.CanViewContent)
        {
            // Mesmo o perfil compartilhado do dono omite listas que ele marcou como privadas.
            var lists = db.Lists.AsNoTracking().Where(l => l.UserId == ownerId && l.IsPublic);
            profile.Lists = await lists.OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id)
                .Skip((listsPage - 1) * PageSize).Take(PageSize)
                .Select(l => new SharedListDto(l.Id, l.Title, l.Description, l.Items.Count)).ToListAsync();
            var reviews = db.Reviews.AsNoTracking().Where(r => r.UserId == ownerId)
                .Join(db.Movies, r => r.MovieId, m => m.Id, (r, m) => new { Review = r, MovieTitle = m.Title });
            profile.Reviews = await reviews.OrderByDescending(r => r.Review.CreatedAt).ThenBy(r => r.Review.Id)
                .Skip((reviewsPage - 1) * PageSize).Take(PageSize)
                .Select(r => new SharedReviewDto(r.Review.Id, r.Review.MovieId, r.MovieTitle,
                    r.Review.Rating, r.Review.Comment, r.Review.CreatedAt)).ToListAsync();
        }
        await tx.CommitAsync();
        return profile;
    }

    public async Task<IReadOnlyList<SocialUserDto>> GetRequestsAsync(Guid ownerId, int offset, int limit) =>
        await db.UserFollowers.Where(f => f.FollowingId == ownerId && !f.IsAccepted)
            .Join(db.Users.Where(u => u.Name != ReservedUserNames.Removed), f => f.FollowerId, u => u.Id,
                (f, u) => new { Follow = f, User = u })
            .OrderBy(r => r.Follow.CreatedAt).ThenBy(r => r.User.Id).Skip(offset).Take(limit)
            .Select(r => new SocialUserDto { Id = r.User.Id, Name = r.User.Name, IsPrivate = r.User.IsPrivate,
                PhotoUrl = r.User.PhotoData != null ? "/api/users/" + r.User.Id + "/photo" : null }).ToListAsync();

    public async Task<bool> AcceptRequestAsync(Guid ownerId, Guid followerId) =>
        await db.UserFollowers.Where(f => f.FollowingId == ownerId && f.FollowerId == followerId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.IsAccepted, true)) > 0;

    public async Task DeclineRequestAsync(Guid ownerId, Guid followerId) =>
        await db.UserFollowers.Where(f => f.FollowingId == ownerId && f.FollowerId == followerId && !f.IsAccepted)
            .ExecuteDeleteAsync();
}

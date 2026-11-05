using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class UserFollowRepository(MoviesArchiveDbContext db) : IUserFollowRepository
{
    public Task<bool> IsActiveUserAsync(Guid userId) =>
        db.Users.AnyAsync(u => u.Id == userId && u.Name != ReservedUserNames.Removed);

    public async Task<IReadOnlyList<SocialUserDto>> FindAsync(Guid viewerId, string search, string scope, int offset, int limit)
    {
        var users = db.Users.AsNoTracking()
            .Where(u => u.Id != viewerId && u.Name != ReservedUserNames.Removed);
        if (search.Length > 0)
            users = users.Where(u => u.Name.ToLower().Contains(search.ToLower()));
        if (scope == "following")
            users = users.Where(u => db.UserFollowers.Any(f => f.FollowerId == viewerId && f.FollowingId == u.Id));
        if (scope == "followers")
            users = users.Where(u => db.UserFollowers.Any(f => f.FollowingId == viewerId && f.FollowerId == u.Id));

        return await users.OrderBy(u => u.Name).ThenBy(u => u.Id).Skip(offset).Take(limit)
            .Select(u => new SocialUserDto
            {
                Id = u.Id,
                Name = u.Name,
                PhotoUrl = u.PhotoData != null ? "/api/users/" + u.Id + "/photo" : null,
                IsFollowing = db.UserFollowers.Any(f => f.FollowerId == viewerId && f.FollowingId == u.Id)
            }).ToListAsync();
    }

    // A chave composta impede duplicatas, inclusive em requisições simultâneas.
    public async Task FollowAsync(Guid followerId, Guid followingId) =>
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "UserFollowers" ("FollowerId", "FollowingId", "CreatedAt")
            VALUES ({followerId}, {followingId}, CURRENT_TIMESTAMP)
            ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING
            """);

    public async Task UnfollowAsync(Guid followerId, Guid followingId) =>
        await db.UserFollowers.Where(f => f.FollowerId == followerId && f.FollowingId == followingId)
            .ExecuteDeleteAsync();
}

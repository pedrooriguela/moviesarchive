using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IUserFollowRepository
{
    Task<bool> IsActiveUserAsync(Guid userId);
    Task<IReadOnlyList<SocialUserDto>> FindAsync(Guid viewerId, string search, string scope, int offset, int limit);
    Task FollowAsync(Guid followerId, Guid followingId);
    Task UnfollowAsync(Guid followerId, Guid followingId);
}

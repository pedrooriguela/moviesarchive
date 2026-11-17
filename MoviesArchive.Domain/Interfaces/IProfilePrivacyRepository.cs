using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IProfilePrivacyRepository
{
    Task<bool> SetPrivacyAsync(Guid userId, bool isPrivate);
    Task<SharedProfileDto?> GetProfileAsync(Guid ownerId, Guid viewerId, int listsPage, int reviewsPage);
    Task<IReadOnlyList<SocialUserDto>> GetRequestsAsync(Guid ownerId, int offset, int limit);
    Task<bool> AcceptRequestAsync(Guid ownerId, Guid followerId);
    Task DeclineRequestAsync(Guid ownerId, Guid followerId);
}

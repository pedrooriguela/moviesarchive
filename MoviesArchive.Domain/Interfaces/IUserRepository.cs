using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<bool> UpdateAsync(Guid id, string name, string password);
    Task<bool> UpdateProfileAsync(Guid id, string name);
    Task<bool> UpdateProfilePhotoAsync(Guid id, byte[] photoData, string contentType);
    Task<bool> RemoveProfilePhotoAsync(Guid id);
    Task<bool> Delete(Guid id);
    Task<User?> GetByIdAsync(Guid id);
    User? GetByUsername(string username);
    User? GetByEmail(string email);
}

using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<bool> UpdateAsync(Guid id, string name, string password);
    Task<bool> Delete(Guid id);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task UpdatePasswordResetTokenAsync(Guid id, string? token, DateTime? expiresAt);
    Task<User?> GetByPasswordResetTokenAsync(string token);
}

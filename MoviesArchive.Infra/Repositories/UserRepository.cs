using MoviesArchive.Infra.Database;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;
using Microsoft.EntityFrameworkCore;


namespace MoviesArchive.Infra.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MoviesArchiveDbContext _dbContext;
    public UserRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(User user)
    {
        await _dbContext.AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(Guid id, string name, string password)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
            return false;

        user.Name = name;
        user.Password = password;
        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task UpdatePasswordResetTokenAsync(Guid id, string? token, DateTime? expiresAt)
    {
        var user = await GetByIdAsync(id)!;
        user.PasswordResetToken = token;
        user.PasswordResetExpiresAt = expiresAt;
        
        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> Delete(Guid id)
    {
        var user = GetByIdAsync(id);
        if (user == null)
            return false;

        _dbContext.Remove(user);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        return user;
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var user = await _dbContext.Users.Where(p => p.Name == username).FirstOrDefaultAsync();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _dbContext.Users.Where(p => p.Email == email).FirstOrDefaultAsync();
        return user;
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token)
    {
        var user = await _dbContext.Users.Where(p => p.PasswordResetToken == token).FirstOrDefaultAsync();
        return user;
    }
}

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

    public async Task<bool> UpdateProfileAsync(Guid id, string name)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
            return false;

        user.Name = name;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfilePhotoAsync(Guid id, byte[] photoData, string contentType)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
            return false;

        user.PhotoData = photoData;
        user.PhotoContentType = contentType;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveProfilePhotoAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user == null)
            return false;

        user.PhotoData = null;
        user.PhotoContentType = null;
        await _dbContext.SaveChangesAsync();
        return true;
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
    
    public User? GetByUsername(string username)
    {
        var user = _dbContext.Users.Where(p => p.Name == username).FirstOrDefault();
        return user;
    }

    public User? GetByEmail(string email)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var user = _dbContext.Users.FirstOrDefault(user => user.Email.ToLower() == normalizedEmail);
        return user;
    }
}

using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Auth;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public RefreshTokenRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
        _dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash);

    public async Task AddAsync(RefreshToken token)
    {
        await _dbContext.RefreshTokens.AddAsync(token);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        if (_dbContext.Entry(token).State == EntityState.Detached)
            _dbContext.RefreshTokens.Update(token);

        await _dbContext.SaveChangesAsync();
    }

    public Task RevokeAllForUserAsync(Guid userId) =>
        _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, (DateTimeOffset?)DateTimeOffset.UtcNow));

    public Task DeleteStaleForUserAsync(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var revokedBefore = now.AddDays(-1);
        return _dbContext.RefreshTokens
            .Where(token => token.UserId == userId &&
                (token.ExpiresAt <= now || (token.RevokedAt != null && token.RevokedAt < revokedBefore)))
            .ExecuteDeleteAsync();
    }
}

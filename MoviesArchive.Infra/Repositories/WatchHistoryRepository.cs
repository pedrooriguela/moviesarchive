using MoviesArchive.Infra.Database;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.History;
using Microsoft.EntityFrameworkCore;

namespace MoviesArchive.Infra.Repositories;

public class WatchHistoryRepository : IWatchHistoryRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public WatchHistoryRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WatchHistoryEntry entry)
    {
        await _dbContext.WatchHistory.AddAsync(entry);
        await _dbContext.SaveChangesAsync();
    }

    // Mais recentes primeiro; o Id desempata entradas com o mesmo horário para a paginação não repetir itens
    public async Task<(List<WatchHistoryEntry> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize)
    {
        var query = _dbContext.WatchHistory
            .AsNoTracking()
            .Where(entry => entry.UserId == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(entry => entry.Movie)
            .OrderByDescending(entry => entry.WatchedAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}

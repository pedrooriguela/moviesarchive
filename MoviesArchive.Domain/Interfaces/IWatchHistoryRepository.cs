using MoviesArchive.Domain.Models.History;

namespace MoviesArchive.Domain.Interfaces;

public interface IWatchHistoryRepository
{
    Task AddAsync(WatchHistoryEntry entry);
    Task<(List<WatchHistoryEntry> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize);
}

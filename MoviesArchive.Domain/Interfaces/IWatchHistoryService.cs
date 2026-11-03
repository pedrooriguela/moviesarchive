using MoviesArchive.Domain.Models.History.Dtos;
using MoviesArchive.Domain.Models.Movies.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IWatchHistoryService
{
    Task<WatchHistoryEntryResponseDto> RecordAsync(Guid userId, RecordWatchRequestDto request);
    Task<PagedResponseDto<WatchHistoryEntryResponseDto>> GetByUserIdAsync(Guid userId, int page, int pageSize);
}

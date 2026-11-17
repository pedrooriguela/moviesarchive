using MoviesArchive.Domain.Models.Admin.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IPlatformStatsService
{
    // periodDays nulo = desde o início
    Task<PlatformStatsDto> GetAsync(int? periodDays);
}

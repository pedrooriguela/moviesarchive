using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;
using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Interfaces;

// Contagens do painel do administrador. Ignoram os filtros globais de filmes:
// entram também os filmes removidos e os de qualquer classificação indicativa.
// "since" nulo = desde o início.
public interface IPlatformStatsRepository
{
    Task<(int Total, int Admins, int Subscribers)> CountUsersAsync();
    Task<(int Active, int Removed)> CountMoviesAsync();
    Task<int> CountAccessesAsync(DateTimeOffset? since);
    Task<int> CountActiveViewersAsync(DateTimeOffset? since);
    Task<List<DateTimeOffset>> GetAccessTimesAsync(DateTimeOffset? since);
    Task<List<MovieAccessCount>> CountAccessesByMovieAsync(DateTimeOffset? since);
    Task<List<Movie>> GetAllMoviesAsync();
    Task<(int Count, decimal? Average)> GetReviewSummaryAsync();
    Task<List<PlanSubscriptionsDto>> CountSubscriptionsByPlanAsync();
    Task<List<(Guid MovieId, int Count)>> CountWatchlistedAsync(int limit);
}

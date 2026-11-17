using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;
using MoviesArchive.Domain.Models.History;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

// Contagens do painel do administrador. IgnoreQueryFilters em tudo: filmes removidos e de
// qualquer classificação indicativa também contam, senão os números mudariam conforme quem olha.
public class PlatformStatsRepository : IPlatformStatsRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public PlatformStatsRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Contas anonimizadas (exclusão que mantém o conteúdo) não são mais usuários
    private IQueryable<User> ActiveUsers() =>
        _dbContext.Users.AsNoTracking().Where(user => user.Name != ReservedUserNames.Removed);

    private IQueryable<WatchHistoryEntry> Accesses(DateTimeOffset? since)
    {
        var query = _dbContext.WatchHistory.IgnoreQueryFilters().AsNoTracking();
        return since is DateTimeOffset start ? query.Where(entry => entry.WatchedAt >= start) : query;
    }

    public async Task<(int Total, int Admins, int Subscribers)> CountUsersAsync()
    {
        var total = await ActiveUsers().CountAsync();
        var admins = await ActiveUsers().CountAsync(user => user.IsAdmin);
        var subscribers = await ActiveUsers().CountAsync(user => user.SubscriptionId != null);
        return (total, admins, subscribers);
    }

    public async Task<(int Active, int Removed)> CountMoviesAsync()
    {
        var movies = _dbContext.Movies.IgnoreQueryFilters().AsNoTracking();
        var active = await movies.CountAsync(movie => movie.DeletedAt == null);
        var removed = await movies.CountAsync(movie => movie.DeletedAt != null);
        return (active, removed);
    }

    public Task<int> CountAccessesAsync(DateTimeOffset? since) =>
        Accesses(since).CountAsync();

    public Task<int> CountActiveViewersAsync(DateTimeOffset? since) =>
        Accesses(since).Select(entry => entry.UserId).Distinct().CountAsync();

    // Só os horários: a separação por dia (no fuso de Brasília) é feita no PlatformStatsService
    public Task<List<DateTimeOffset>> GetAccessTimesAsync(DateTimeOffset? since) =>
        Accesses(since).Select(entry => entry.WatchedAt).ToListAsync();

    public async Task<List<MovieAccessCount>> CountAccessesByMovieAsync(DateTimeOffset? since)
    {
        var rows = await Accesses(since)
            .GroupBy(entry => entry.MovieId)
            .Select(group => new
            {
                MovieId = group.Key,
                Count = group.Count(),
                Viewers = group.Select(entry => entry.UserId).Distinct().Count(),
            })
            .ToListAsync();

        return rows.Select(row => new MovieAccessCount(row.MovieId, row.Count, row.Viewers)).ToList();
    }

    public Task<List<Movie>> GetAllMoviesAsync() =>
        _dbContext.Movies.IgnoreQueryFilters().AsNoTracking().ToListAsync();

    public async Task<(int Count, decimal? Average)> GetReviewSummaryAsync()
    {
        var count = await _dbContext.Reviews.CountAsync();
        var average = await _dbContext.Reviews.AverageAsync(review => (decimal?)review.Rating);
        return (count, average is decimal value ? Math.Round(value, 2) : null);
    }

    // Assinaturas ativas por plano e quantas pessoas (titular + convidados) cada plano atende
    public async Task<List<PlanSubscriptionsDto>> CountSubscriptionsByPlanAsync()
    {
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.EndedAt == null)
            .GroupBy(subscription => subscription.PlanId)
            .Select(group => new { PlanId = group.Key, Count = group.Count() })
            .ToListAsync();

        var members = await (
                from user in ActiveUsers()
                join subscription in _dbContext.Subscriptions on user.SubscriptionId equals subscription.Id
                where subscription.EndedAt == null
                group user by subscription.PlanId into planGroup
                select new { PlanId = planGroup.Key, Count = planGroup.Count() })
            .ToListAsync();

        var plans = await _dbContext.Plans.AsNoTracking().OrderBy(plan => plan.Price).ToListAsync();

        return plans
            .Select(plan => new PlanSubscriptionsDto
            {
                PlanName = plan.Name,
                Subscriptions = subscriptions.FirstOrDefault(row => row.PlanId == plan.Id)?.Count ?? 0,
                Members = members.FirstOrDefault(row => row.PlanId == plan.Id)?.Count ?? 0,
            })
            .ToList();
    }

    // Em quantas watchlists cada filme está (os mais guardados primeiro)
    public async Task<List<(Guid MovieId, int Count)>> CountWatchlistedAsync(int limit)
    {
        var rows = await _dbContext.ListMovies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => _dbContext.Lists.Any(list => list.Id == item.ListId && list.IsWatchlist))
            .GroupBy(item => item.MovieId)
            .Select(group => new { MovieId = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count)
            .Take(limit)
            .ToListAsync();

        return rows.Select(row => (row.MovieId, row.Count)).ToList();
    }
}

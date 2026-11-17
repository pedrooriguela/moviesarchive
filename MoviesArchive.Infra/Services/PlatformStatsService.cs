using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Admin;
using MoviesArchive.Domain.Models.Admin.Dtos;
using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Infra.Services;

// Monta o painel de estatísticas. "Acesso" = um clique no play (uma linha de WatchHistory).
public class PlatformStatsService : IPlatformStatsService
{
    private const int TopWatchlistedSize = 5;

    // Os dias do gráfico seguem o horário de Brasília, não o UTC do banco
    private static readonly TimeZoneInfo BrazilTime = FindBrazilTime();

    private readonly IPlatformStatsRepository _statsRepository;

    public PlatformStatsService(
        IPlatformStatsRepository statsRepository)
    {
        _statsRepository = statsRepository;
    }

    public async Task<PlatformStatsDto> GetAsync(int? periodDays)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, BrazilTime).DateTime);

        // Últimos N dias contando hoje, a partir da meia-noite de Brasília do primeiro dia
        DateTimeOffset? since = periodDays is int days
            ? StartOfDay(today.AddDays(1 - days))
            : null;

        var (totalUsers, adminUsers, subscribers) = await _statsRepository.CountUsersAsync();
        var (activeMovies, removedMovies) = await _statsRepository.CountMoviesAsync();
        var (totalReviews, averageRating) = await _statsRepository.GetReviewSummaryAsync();

        var movies = (await _statsRepository.GetAllMoviesAsync()).ToDictionary(movie => movie.Id);
        var byMovie = await _statsRepository.CountAccessesByMovieAsync(since);
        var accessTimes = await _statsRepository.GetAccessTimesAsync(since);

        return new PlatformStatsDto
        {
            PeriodDays = periodDays,
            PeriodStart = since,
            GeneratedAt = now,
            TotalUsers = totalUsers,
            AdminUsers = adminUsers,
            Subscribers = subscribers,
            ActiveMovies = activeMovies,
            RemovedMovies = removedMovies,
            Accesses = accessTimes.Count,
            AllTimeAccesses = since == null ? accessTimes.Count : await _statsRepository.CountAccessesAsync(null),
            ActiveViewers = await _statsRepository.CountActiveViewersAsync(since),
            TotalReviews = totalReviews,
            AverageRating = averageRating,
            TimelineUnit = periodDays == null ? "month" : "day",
            Timeline = periodDays == null
                ? MonthlyTimeline(accessTimes, today)
                : DailyTimeline(accessTimes, today, periodDays.Value),
            AccessesByGenre = AccessesByGenre(byMovie, movies),
            AccessesByMovie = AccessesByMovie(byMovie, movies),
            SubscriptionsByPlan = await _statsRepository.CountSubscriptionsByPlanAsync(),
            TopWatchlisted = (await _statsRepository.CountWatchlistedAsync(TopWatchlistedSize))
                .Where(row => movies.ContainsKey(row.MovieId))
                .Select(row => ToMovieAccess(movies[row.MovieId], row.Count, viewers: 0))
                .ToList(),
        };
    }

    // Um ponto por dia do período, inclusive os dias sem acesso
    private static List<TimelinePointDto> DailyTimeline(List<DateTimeOffset> times, DateOnly today, int days)
    {
        var counts = times
            .GroupBy(LocalDate)
            .ToDictionary(group => group.Key, group => group.Count());

        return Enumerable.Range(0, days)
            .Select(offset => today.AddDays(offset - days + 1))
            .Select(date => new TimelinePointDto { Date = date, Count = counts.GetValueOrDefault(date) })
            .ToList();
    }

    // Um ponto por mês, do primeiro acesso até o mês atual (no mínimo os últimos 6 meses)
    private static List<TimelinePointDto> MonthlyTimeline(List<DateTimeOffset> times, DateOnly today)
    {
        var counts = times
            .GroupBy(time => FirstOfMonth(LocalDate(time)))
            .ToDictionary(group => group.Key, group => group.Count());

        var current = FirstOfMonth(today);
        var first = counts.Count > 0 ? counts.Keys.Min() : current;
        var sixMonthsAgo = current.AddMonths(-5);
        if (first > sixMonthsAgo)
            first = sixMonthsAgo;

        var points = new List<TimelinePointDto>();
        for (var month = first; month <= current; month = month.AddMonths(1))
            points.Add(new TimelinePointDto { Date = month, Count = counts.GetValueOrDefault(month) });

        return points;
    }

    // Todos os gêneros aparecem, mesmo sem acesso; um filme com dois gêneros soma nos dois
    private static List<GenreAccessDto> AccessesByGenre(
        List<MovieAccessCount> byMovie, Dictionary<Guid, Movie> movies)
    {
        var totals = Enum.GetValues<Genre>().ToDictionary(genre => genre.ToString(), _ => 0);

        foreach (var row in byMovie)
        {
            if (!movies.TryGetValue(row.MovieId, out var movie))
                continue;

            foreach (var code in movie.Genres.Distinct())
            {
                if (totals.ContainsKey(code))
                    totals[code] += row.Count;
            }
        }

        return Enum.GetValues<Genre>()
            .Select(genre => new GenreAccessDto
            {
                Genre = genre.ToString(),
                Label = genre.ToDisplayName(),
                Count = totals[genre.ToString()],
            })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Label)
            .ToList();
    }

    // Filmes do catálogo (mesmo sem acesso) e removidos que tiveram acesso no período
    private static List<MovieAccessDto> AccessesByMovie(
        List<MovieAccessCount> byMovie, Dictionary<Guid, Movie> movies)
    {
        var counts = byMovie.ToDictionary(row => row.MovieId);

        return movies.Values
            .Where(movie => !movie.IsRemoved || counts.ContainsKey(movie.Id))
            .Select(movie =>
            {
                var row = counts.GetValueOrDefault(movie.Id);
                return ToMovieAccess(movie, row?.Count ?? 0, row?.Viewers ?? 0);
            })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.Title)
            .ToList();
    }

    private static MovieAccessDto ToMovieAccess(Movie movie, int count, int viewers) => new()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        ReleaseYear = movie.ReleaseDate?.Year,
        IsRemoved = movie.IsRemoved,
        Count = count,
        Viewers = viewers,
    };

    private static DateOnly LocalDate(DateTimeOffset time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time, BrazilTime).DateTime);

    private static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    private static DateTimeOffset StartOfDay(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, BrazilTime.GetUtcOffset(local)).ToUniversalTime();
    }

    // "America/Sao_Paulo" existe no Linux e no Windows com .NET 6+; sem ele, UTC-3 fixo
    private static TimeZoneInfo FindBrazilTime()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception)
        {
            return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        }
    }
}

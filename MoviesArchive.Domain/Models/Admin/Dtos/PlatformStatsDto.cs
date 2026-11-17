namespace MoviesArchive.Domain.Models.Admin.Dtos;

// Estatísticas de uso da plataforma (painel do administrador).
// "Acesso" = um clique no play, ou seja, uma linha da tabela WatchHistory.
public class PlatformStatsDto
{
    // Período das contagens de acesso: últimos N dias (contando hoje). Nulo = desde o início.
    public int? PeriodDays { get; set; }
    public DateTimeOffset? PeriodStart { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    // Contas ativas (sem as excluídas e anonimizadas)
    public int TotalUsers { get; set; }
    public int AdminUsers { get; set; }

    // Pessoas com assinatura ativa, titulares e convidadas
    public int Subscribers { get; set; }

    public int ActiveMovies { get; set; }
    public int RemovedMovies { get; set; }

    // Acessos no período e desde o início
    public int Accesses { get; set; }
    public int AllTimeAccesses { get; set; }

    // Pessoas diferentes que assistiram alguma coisa no período
    public int ActiveViewers { get; set; }

    public int TotalReviews { get; set; }
    public decimal? AverageRating { get; set; }

    // "day" para períodos em dias; "month" para o período todo
    public string TimelineUnit { get; set; } = "day";
    public List<TimelinePointDto> Timeline { get; set; } = [];

    // Um filme com dois gêneros conta nos dois
    public List<GenreAccessDto> AccessesByGenre { get; set; } = [];

    // Todos os filmes do catálogo e os removidos que tiveram acesso no período, do mais acessado ao menos
    public List<MovieAccessDto> AccessesByMovie { get; set; } = [];

    public List<PlanSubscriptionsDto> SubscriptionsByPlan { get; set; } = [];

    // Filmes que mais aparecem nas watchlists
    public List<MovieAccessDto> TopWatchlisted { get; set; } = [];
}

public class TimelinePointDto
{
    // Dia (ou primeiro dia do mês), no horário de Brasília
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class GenreAccessDto
{
    public string Genre { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MovieAccessDto
{
    public Guid MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public bool IsRemoved { get; set; }

    // Acessos no período (ou, em TopWatchlisted, quantas watchlists têm o filme)
    public int Count { get; set; }

    // Pessoas diferentes que assistiram no período
    public int Viewers { get; set; }
}

public class PlanSubscriptionsDto
{
    public string PlanName { get; set; } = string.Empty;
    public int Subscriptions { get; set; }
    public int Members { get; set; }
}

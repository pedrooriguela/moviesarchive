using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Lists;

public class MovieList : Entity
{
    // Nome fixo da watchlist. Nenhuma outra lista pode usar esse nome.
    public const string WatchlistTitle = "Watchlist";

    public MovieList(
        Guid userId,
        string title,
        string? description,
        bool isPublic)
    {
        UserId = userId;
        Title = title;
        Description = description;
        IsPublic = isPublic;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // A watchlist é uma lista como as outras, criada sozinha para cada usuário (no máximo uma).
    // Só não pode ser renomeada nem excluída.
    public bool IsWatchlist { get; set; }

    public List<ListMovie> Items { get; set; } = new();

    public bool IsPrivate => !IsPublic;

    public bool IsOwnedBy(Guid userId) =>
        UserId == userId;

    public static bool IsReservedTitle(string title) =>
        string.Equals(title.Trim(), WatchlistTitle, StringComparison.OrdinalIgnoreCase);

    public class Builder
    {
        public static MovieList Create(Guid userId, string title, string? description, bool isPublic) =>
            new(userId, title, description, isPublic);

        // Começa privada, como a watchlist antiga
        public static MovieList CreateWatchlist(Guid userId) =>
            new(userId, WatchlistTitle, null, isPublic: false) { IsWatchlist = true };
    }
}

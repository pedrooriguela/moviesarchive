namespace MoviesArchive.Domain.Models.Watchlist;

// Filme que o usuário quer assistir. Chave composta (UserId, MovieId).
public class WatchlistItem
{
    public WatchlistItem(Guid userId, Guid movieId)
    {
        UserId = userId;
        MovieId = movieId;
        AddedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

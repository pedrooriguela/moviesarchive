using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Models.History;

// Uma entrada por clique no play: assistir o mesmo filme de novo gera outra entrada
public class WatchHistoryEntry : Entity
{
    public WatchHistoryEntry(Guid userId, Guid movieId)
    {
        UserId = userId;
        MovieId = movieId;
        WatchedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public DateTimeOffset WatchedAt { get; set; }
    public Movie? Movie { get; set; }

    public class Builder
    {
        public static WatchHistoryEntry Create(Guid userId, Guid movieId) =>
            new(userId, movieId);
    }
}

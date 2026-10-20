using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Models.Lists;

public class ListMovie
{
    public ListMovie(Guid listId, Guid movieId, int position)
    {
        if (position < 0)
            throw new ArgumentOutOfRangeException(nameof(position));

        ListId = listId;
        MovieId = movieId;
        Position = position;
        AddedAt = DateTimeOffset.UtcNow;
    }

    public Guid ListId { get; set; }
    public Guid MovieId { get; set; }
    public int Position { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public Movie? Movie { get; set; }
}

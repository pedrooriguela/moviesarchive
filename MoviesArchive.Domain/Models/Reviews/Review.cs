using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Reviews;

public class Review : Entity
{
    public Review(Guid userId, Guid movieId, int rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating));

        UserId = userId;
        MovieId = movieId;
        Rating = rating;
        Comment = comment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
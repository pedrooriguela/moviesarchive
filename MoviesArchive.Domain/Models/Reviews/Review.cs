using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Exceptions;

namespace MoviesArchive.Domain.Models.Reviews;

public class Review : Entity
{
    public const decimal MinRating = 0.5m;
    public const decimal MaxRating = 5m;
    public const int MaxCommentLength = 2000;

    public Review(Guid userId, Guid movieId, decimal rating, string? comment)
    {
        EnsureValidRating(rating);

        UserId = userId;
        MovieId = movieId;
        Rating = rating;
        Comment = NormalizeComment(comment);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }

    // De 0,5 a 5, em passos de meia estrela
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void Update(decimal rating, string? comment)
    {
        EnsureValidRating(rating);
        Rating = rating;
        Comment = NormalizeComment(comment);
    }

    public static bool IsValidRating(decimal rating) =>
        rating >= MinRating && rating <= MaxRating && rating * 2 == decimal.Truncate(rating * 2);

    private static void EnsureValidRating(decimal rating)
    {
        if (!IsValidRating(rating))
            throw new InvalidReviewRatingException();
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return null;

        var trimmed = comment.Trim();
        if (trimmed.Length > MaxCommentLength)
            throw new ReviewCommentTooLongException(MaxCommentLength);

        return trimmed;
    }
}

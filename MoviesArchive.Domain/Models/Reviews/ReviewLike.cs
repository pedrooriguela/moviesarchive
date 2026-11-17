namespace MoviesArchive.Domain.Models.Reviews;

// Curtida em uma avaliação (tabela "ReviewLikes"). Chave composta (UserId, ReviewId).
public class ReviewLike
{
    public ReviewLike(Guid userId, Guid reviewId)
    {
        UserId = userId;
        ReviewId = reviewId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public Guid ReviewId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

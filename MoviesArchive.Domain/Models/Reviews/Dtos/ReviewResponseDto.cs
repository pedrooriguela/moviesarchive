namespace MoviesArchive.Domain.Models.Reviews.Dtos;

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserPhotoUrl { get; set; }
    public decimal Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

namespace MoviesArchive.Domain.Models.Reviews.Dtos;

public class ReplyResponseDto
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid? ParentReplyId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserPhotoUrl { get; set; }

    // Vazio quando a resposta foi excluída (IsDeleted)
    public string Comment { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
}

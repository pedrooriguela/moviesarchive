namespace MoviesArchive.Domain.Models.Notifications.Dtos;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorPhotoUrl { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public Guid ReviewId { get; set; }
    public Guid? ReplyId { get; set; }

    // Começo do texto: da resposta nova (respostas) ou do que foi curtido (curtidas)
    public string? Excerpt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

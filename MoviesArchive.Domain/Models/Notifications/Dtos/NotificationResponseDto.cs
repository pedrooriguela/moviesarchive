namespace MoviesArchive.Domain.Models.Notifications.Dtos;

public class NotificationResponseDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorPhotoUrl { get; set; }

    // Avisos de avaliação (nulos nos avisos de plano)
    public Guid? MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public Guid? ReviewId { get; set; }
    public Guid? ReplyId { get; set; }

    // Começo do texto: da resposta nova (respostas) ou do que foi curtido (curtidas)
    public string? Excerpt { get; set; }

    // Avisos de plano (nulos nos avisos de avaliação)
    public Guid? InviteId { get; set; }
    public string? PlanName { get; set; }

    // Situação atual do convite: Pending (dá para aceitar ou recusar), Accepted, Declined ou
    // Closed (venceu, foi cancelado ou o plano acabou)
    public string? InviteStatus { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

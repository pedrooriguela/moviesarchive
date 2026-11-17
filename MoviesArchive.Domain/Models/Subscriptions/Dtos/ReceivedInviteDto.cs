namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

// Convite pendente, visto por quem foi convidado
public class ReceivedInviteDto
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

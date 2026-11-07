namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

// Convite pendente, visto pelo titular
public class SentInviteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

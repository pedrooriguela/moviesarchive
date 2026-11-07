namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public PlanResponseDto Plan { get; set; } = new();
    public DateTimeOffset StartedAt { get; set; }
    public bool IsOwner { get; set; }
    public string OwnerName { get; set; } = string.Empty;

    // Titular primeiro
    public List<SubscriptionMemberDto> Members { get; set; } = [];

    // Só preenchido para o titular, que é quem administra os convites
    public List<SentInviteDto> PendingInvites { get; set; } = [];

    // Vagas livres: máximo do plano menos integrantes e convites pendentes
    public int AvailableSlots { get; set; }
}

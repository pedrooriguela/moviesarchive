using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Subscriptions;

// Convite do titular de um plano familiar para outro usuário (achado pelo nome de usuário)
public class FamilyInvite : Entity
{
    public static readonly TimeSpan Validity = TimeSpan.FromDays(7);

    public FamilyInvite(Guid subscriptionId, Guid invitedUserId)
    {
        SubscriptionId = subscriptionId;
        InvitedUserId = invitedUserId;
        Status = InviteStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = CreatedAt + Validity;
    }

    public Guid SubscriptionId { get; set; }
    public Guid InvitedUserId { get; set; }
    public InviteStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }

    // Pendente e ainda dentro da validade: é o que conta para o limite de integrantes
    public bool IsPendingAt(DateTimeOffset now) =>
        Status == InviteStatus.Pending && ExpiresAt > now;

    public void Respond(InviteStatus status)
    {
        if (Status != InviteStatus.Pending)
            throw new InvalidOperationException("Este convite já foi respondido.");
        if (status == InviteStatus.Pending)
            throw new ArgumentException("A resposta precisa ser aceitar, recusar ou cancelar.", nameof(status));

        Status = status;
        RespondedAt = DateTimeOffset.UtcNow;
    }
}

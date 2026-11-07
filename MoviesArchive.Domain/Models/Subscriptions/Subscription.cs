using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Subscriptions;

// Assinatura de um titular. Nada é apagado: EndedAt nulo significa ativa.
// Os integrantes são os usuários com Users.SubscriptionId apontando para ela (titular incluído).
public class Subscription : Entity
{
    public Subscription(Guid ownerId, Guid planId)
    {
        OwnerId = ownerId;
        PlanId = planId;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public Guid OwnerId { get; set; }
    public Guid PlanId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public SubscriptionEndReason? EndReason { get; set; }

    public bool IsActive => EndedAt == null;

    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    // Quem chama também precisa zerar Users.SubscriptionId de todos os integrantes
    public void End(SubscriptionEndReason reason)
    {
        if (!IsActive)
            throw new InvalidOperationException("A assinatura já foi encerrada.");

        EndedAt = DateTimeOffset.UtcNow;
        EndReason = reason;
    }
}

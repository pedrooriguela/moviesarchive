using MoviesArchive.Domain.Models.Plans;
using MoviesArchive.Domain.Models.Subscriptions;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<List<Plan>> GetActivePlansAsync();
    Task<Plan?> GetPlanAsync(Guid planId);
    Task<Subscription?> GetActiveAsync(Guid subscriptionId);
    Task<List<SubscriptionMember>> GetMembersAsync(Guid subscriptionId);
    Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds);

    // Pendentes e dentro da validade
    Task<List<FamilyInvite>> GetValidPendingInvitesAsync(Guid subscriptionId, DateTimeOffset now);
    Task<List<FamilyInvite>> GetValidReceivedInvitesAsync(Guid userId, DateTimeOffset now);

    Task<FamilyInvite?> GetInviteAsync(Guid inviteId);

    // Pendente, vencido ou não
    Task<FamilyInvite?> GetPendingInviteAsync(Guid subscriptionId, Guid invitedUserId);

    // Escritas que mexem em várias linhas rodam numa transação só
    Task CreateAsync(Subscription subscription, User owner);
    Task<Subscription> ChangePlanAsync(Subscription current, Guid newPlanId);

    // Encerra a assinatura, tira todos os integrantes e cancela os convites pendentes
    Task EndAsync(Subscription subscription, SubscriptionEndReason reason);

    // Conferem a vaga com a assinatura travada, para dois pedidos simultâneos não passarem do limite
    Task<bool> TryAddInviteAsync(FamilyInvite invite, int maxMembers, FamilyInvite? expiredInviteToCancel);
    Task<InviteAcceptResult> TryAcceptInviteAsync(FamilyInvite invite, User user, int maxMembers);

    Task SaveChangesAsync();
}

using MoviesArchive.Domain.Models.Subscriptions.Dtos;

namespace MoviesArchive.Domain.Interfaces;

// Regras de planos e assinaturas. Erros de regra saem como SubscriptionException.
public interface ISubscriptionService
{
    Task<List<PlanResponseDto>> GetPlansAsync();
    Task<SubscriptionStateDto> GetStateAsync(Guid userId);
    Task<SubscriptionStateDto> SubscribeAsync(Guid userId, Guid planId);
    Task<SubscriptionStateDto> ChangePlanAsync(Guid userId, Guid planId);
    Task<SubscriptionStateDto> CancelAsync(Guid userId);
    Task<SubscriptionStateDto> LeaveAsync(Guid userId);
    Task<SubscriptionStateDto> InviteAsync(Guid ownerId, string userName);
    Task<SubscriptionStateDto> CancelInviteAsync(Guid ownerId, Guid inviteId);
    Task<SubscriptionStateDto> RemoveMemberAsync(Guid ownerId, Guid memberId);
    Task<SubscriptionStateDto> AcceptInviteAsync(Guid userId, Guid inviteId);
    Task<SubscriptionStateDto> DeclineInviteAsync(Guid userId, Guid inviteId);
}

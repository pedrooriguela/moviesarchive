using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Plans;
using MoviesArchive.Domain.Models.Subscriptions;
using MoviesArchive.Domain.Models.Subscriptions.Dtos;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Infra.Services;

// Regras:
// - só maiores de 18 contratam; sem data de nascimento não dá para contratar nem aceitar convite
// - cada pessoa está em no máximo uma assinatura (Users.SubscriptionId)
// - familiar: até MaxMembers pessoas, titular incluído, contando os convites pendentes válidos
// - quem já tem plano precisa cancelá-lo (ou sair do atual) antes de aceitar um convite
public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IUserRepository _users;

    public SubscriptionService(
        ISubscriptionRepository subscriptions,
        IUserRepository users)
    {
        _subscriptions = subscriptions;
        _users = users;
    }

    public async Task<List<PlanResponseDto>> GetPlansAsync() =>
        (await _subscriptions.GetActivePlansAsync()).Select(ToPlanDto).ToList();

    public async Task<SubscriptionStateDto> GetStateAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        var today = BirthDates.Today();
        var now = DateTimeOffset.UtcNow;

        SubscriptionResponseDto? subscriptionDto = null;
        var subscription = await GetActiveSubscriptionOfAsync(user);
        if (subscription != null)
            subscriptionDto = await ToSubscriptionDtoAsync(subscription, userId, today, now);

        var receivedInvites = new List<ReceivedInviteDto>();
        foreach (var invite in await _subscriptions.GetValidReceivedInvitesAsync(userId, now))
        {
            var invitingSubscription = await _subscriptions.GetActiveAsync(invite.SubscriptionId);
            if (invitingSubscription == null)
                continue;

            var plan = await _subscriptions.GetPlanAsync(invitingSubscription.PlanId);
            var names = await _subscriptions.GetUserNamesAsync(new List<Guid> { invitingSubscription.OwnerId });

            receivedInvites.Add(new ReceivedInviteDto
            {
                Id = invite.Id,
                OwnerName = names.GetValueOrDefault(invitingSubscription.OwnerId, string.Empty),
                PlanName = plan?.Name ?? string.Empty,
                CreatedAt = invite.CreatedAt,
                ExpiresAt = invite.ExpiresAt,
            });
        }

        var isAdult = user.IsAdultOn(today);
        return new SubscriptionStateDto
        {
            Subscription = subscriptionDto,
            ReceivedInvites = receivedInvites,
            HasBirthDate = user.BirthDate != null,
            IsAdult = isAdult,
            CanSubscribe = subscriptionDto == null && isAdult,
        };
    }

    public async Task<SubscriptionStateDto> SubscribeAsync(Guid userId, Guid planId)
    {
        var user = await GetUserAsync(userId);

        EnsureHasBirthDate(user, "contratar um plano");
        if (!user.IsAdultOn(BirthDates.Today()))
            throw Forbidden("minor", "Somente maiores de 18 anos podem contratar planos.");
        EnsureNotSubscribed(user);

        var plan = await GetPlanForSaleAsync(planId);
        await _subscriptions.CreateAsync(new Subscription(user.Id, plan.Id), user);

        return await GetStateAsync(userId);
    }

    public async Task<SubscriptionStateDto> ChangePlanAsync(Guid userId, Guid planId)
    {
        var (_, subscription) = await GetOwnedSubscriptionAsync(userId, "trocar o plano");
        var plan = await GetPlanForSaleAsync(planId);

        if (plan.Id == subscription.PlanId)
            throw Conflict("same-plan", "Você já está neste plano.");

        // Quem já está (ou foi convidado) precisa caber no novo plano
        var members = (await _subscriptions.GetMembersAsync(subscription.Id)).Count;
        var pending = (await _subscriptions.GetValidPendingInvitesAsync(subscription.Id, DateTimeOffset.UtcNow)).Count;
        if (members + pending > plan.MaxMembers)
            throw Conflict("too-many-members",
                $"O plano {plan.Name} permite {People(plan.MaxMembers)}. Remova integrantes ou cancele convites pendentes antes de trocar.");

        await _subscriptions.ChangePlanAsync(subscription, plan.Id);
        return await GetStateAsync(userId);
    }

    public async Task<SubscriptionStateDto> CancelAsync(Guid userId)
    {
        var (_, subscription) = await GetOwnedSubscriptionAsync(userId, "cancelar a assinatura");

        // Todos os integrantes voltam a ser contas gratuitas
        await _subscriptions.EndAsync(subscription, SubscriptionEndReason.Canceled);
        return await GetStateAsync(userId);
    }

    public async Task<SubscriptionStateDto> LeaveAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        var subscription = await GetActiveSubscriptionOfAsync(user)
            ?? throw NotFound("no-subscription", "Você não faz parte de nenhuma assinatura.");

        if (subscription.IsOwnedBy(userId))
            throw Conflict("owner-cannot-leave", "O titular não sai do plano: para encerrá-lo, cancele a assinatura.");

        user.SubscriptionId = null;
        await _subscriptions.SaveChangesAsync();
        return await GetStateAsync(userId);
    }

    public async Task<SubscriptionStateDto> InviteAsync(Guid ownerId, string userName)
    {
        var (owner, subscription) = await GetOwnedSubscriptionAsync(ownerId, "convidar pessoas");
        var plan = await GetPlanOfAsync(subscription);

        if (plan.Type != PlanType.Family)
            throw Conflict("not-family", "Convites só existem no plano familiar.");

        var name = userName.Trim();
        var invitee = ReservedUserNames.IsReserved(name) ? null : await _users.GetByUsernameAsync(name);
        if (invitee == null)
            throw NotFound("user-not-found", "Nenhum usuário com esse nome.");

        if (invitee.Id == owner.Id)
            throw Conflict("self-invite", "Você já faz parte do plano.");

        if (invitee.SubscriptionId == subscription.Id)
            throw Conflict("already-member", $"{invitee.Name} já faz parte do seu plano.");

        var existing = await _subscriptions.GetPendingInviteAsync(subscription.Id, invitee.Id);
        if (existing != null && existing.IsPendingAt(DateTimeOffset.UtcNow))
            throw Conflict("already-invited", $"{invitee.Name} já tem um convite pendente.");

        // Quem já tem plano pode ser convidado; só não consegue aceitar antes de cancelar o próprio
        var added = await _subscriptions.TryAddInviteAsync(
            new FamilyInvite(subscription.Id, invitee.Id), plan.MaxMembers, existing);
        if (!added)
            throw Conflict("plan-full",
                $"O plano já tem {People(plan.MaxMembers)}, contando os convites pendentes.");

        return await GetStateAsync(ownerId);
    }

    public async Task<SubscriptionStateDto> CancelInviteAsync(Guid ownerId, Guid inviteId)
    {
        var (_, subscription) = await GetOwnedSubscriptionAsync(ownerId, "cancelar convites");

        var invite = await _subscriptions.GetInviteAsync(inviteId);
        if (invite == null || invite.SubscriptionId != subscription.Id)
            throw NotFound("invite-not-found", "Convite não encontrado.");

        if (invite.Status != InviteStatus.Pending)
            throw Conflict("invite-closed", "Este convite já foi respondido.");

        invite.Respond(InviteStatus.Canceled);
        await _subscriptions.SaveChangesAsync();
        return await GetStateAsync(ownerId);
    }

    public async Task<SubscriptionStateDto> RemoveMemberAsync(Guid ownerId, Guid memberId)
    {
        var (_, subscription) = await GetOwnedSubscriptionAsync(ownerId, "remover integrantes");

        if (memberId == ownerId)
            throw Conflict("owner-cannot-leave", "O titular não sai do plano: para encerrá-lo, cancele a assinatura.");

        var member = await _users.GetByIdAsync(memberId);
        if (member == null || member.SubscriptionId != subscription.Id)
            throw NotFound("member-not-found", "Essa pessoa não faz parte do seu plano.");

        member.SubscriptionId = null;
        await _subscriptions.SaveChangesAsync();
        return await GetStateAsync(ownerId);
    }

    public async Task<SubscriptionStateDto> AcceptInviteAsync(Guid userId, Guid inviteId)
    {
        var user = await GetUserAsync(userId);
        var invite = await GetReceivedInviteAsync(userId, inviteId);

        if (!invite.IsPendingAt(DateTimeOffset.UtcNow))
            throw InviteClosed();

        EnsureHasBirthDate(user, "aceitar o convite");
        EnsureNotSubscribed(user);

        var subscription = await _subscriptions.GetActiveAsync(invite.SubscriptionId) ?? throw InviteClosed();
        var plan = await GetPlanOfAsync(subscription);

        var result = await _subscriptions.TryAcceptInviteAsync(invite, user, plan.MaxMembers);
        return result switch
        {
            InviteAcceptResult.Accepted => await GetStateAsync(userId),
            InviteAcceptResult.PlanFull => throw Conflict("plan-full", "Este plano já está completo."),
            InviteAcceptResult.AlreadySubscribed => throw AlreadySubscribed(),
            _ => throw InviteClosed(),
        };
    }

    public async Task<SubscriptionStateDto> DeclineInviteAsync(Guid userId, Guid inviteId)
    {
        var invite = await GetReceivedInviteAsync(userId, inviteId);

        if (invite.Status != InviteStatus.Pending)
            throw Conflict("invite-closed", "Este convite já foi respondido.");

        invite.Respond(InviteStatus.Declined);
        await _subscriptions.SaveChangesAsync();
        return await GetStateAsync(userId);
    }

    // ---------- apoio ----------

    private async Task<User> GetUserAsync(Guid userId) =>
        await _users.GetByIdAsync(userId) ?? throw NotFound("account-not-found", "Conta não encontrada.");

    private async Task<Subscription?> GetActiveSubscriptionOfAsync(User user) =>
        user.SubscriptionId is Guid subscriptionId ? await _subscriptions.GetActiveAsync(subscriptionId) : null;

    private async Task<(User User, Subscription Subscription)> GetOwnedSubscriptionAsync(Guid userId, string action)
    {
        var user = await GetUserAsync(userId);
        var subscription = await GetActiveSubscriptionOfAsync(user)
            ?? throw NotFound("no-subscription", "Você não tem uma assinatura ativa.");

        if (!subscription.IsOwnedBy(userId))
            throw Forbidden("not-owner", $"Só o titular pode {action}.");

        return (user, subscription);
    }

    private async Task<Plan> GetPlanForSaleAsync(Guid planId)
    {
        var plan = await _subscriptions.GetPlanAsync(planId);
        if (plan == null || !plan.IsActive)
            throw NotFound("plan-not-found", "Plano não encontrado.");

        return plan;
    }

    private async Task<Plan> GetPlanOfAsync(Subscription subscription) =>
        await _subscriptions.GetPlanAsync(subscription.PlanId)
            ?? throw new InvalidOperationException("Assinatura sem plano.");

    // Convite inexistente ou de outra pessoa: as duas respostas são iguais
    private async Task<FamilyInvite> GetReceivedInviteAsync(Guid userId, Guid inviteId)
    {
        var invite = await _subscriptions.GetInviteAsync(inviteId);
        if (invite == null || invite.InvitedUserId != userId)
            throw NotFound("invite-not-found", "Convite não encontrado.");

        return invite;
    }

    private async Task<SubscriptionResponseDto> ToSubscriptionDtoAsync(
        Subscription subscription, Guid viewerId, DateOnly today, DateTimeOffset now)
    {
        var plan = await GetPlanOfAsync(subscription);
        var members = await _subscriptions.GetMembersAsync(subscription.Id);
        var pending = await _subscriptions.GetValidPendingInvitesAsync(subscription.Id, now);
        var isOwner = subscription.IsOwnedBy(viewerId);

        var nameIds = pending.Select(invite => invite.InvitedUserId).Append(subscription.OwnerId).ToList();
        var names = await _subscriptions.GetUserNamesAsync(nameIds);

        return new SubscriptionResponseDto
        {
            Id = subscription.Id,
            Plan = ToPlanDto(plan),
            StartedAt = subscription.StartedAt,
            IsOwner = isOwner,
            OwnerName = names.GetValueOrDefault(subscription.OwnerId, string.Empty),
            Members = members
                .OrderByDescending(member => member.UserId == subscription.OwnerId)
                .ThenBy(member => member.Name)
                .Select(member => new SubscriptionMemberDto
                {
                    UserId = member.UserId,
                    Name = member.Name,
                    // Mesma rota usada pelo UserController
                    PhotoUrl = member.HasPhoto ? $"/api/users/{member.UserId}/photo" : null,
                    IsOwner = member.UserId == subscription.OwnerId,
                    IsMinor = member.IsMinorOn(today),
                })
                .ToList(),
            PendingInvites = isOwner
                ? pending.Select(invite => new SentInviteDto
                {
                    Id = invite.Id,
                    UserId = invite.InvitedUserId,
                    UserName = names.GetValueOrDefault(invite.InvitedUserId, string.Empty),
                    CreatedAt = invite.CreatedAt,
                    ExpiresAt = invite.ExpiresAt,
                }).ToList()
                : new List<SentInviteDto>(),
            AvailableSlots = Math.Max(0, plan.MaxMembers - members.Count - pending.Count),
        };
    }

    private static PlanResponseDto ToPlanDto(Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Type = plan.Type.ToString(),
        Price = plan.Price,
        MaxMembers = plan.MaxMembers,
    };

    private static void EnsureHasBirthDate(User user, string action)
    {
        if (user.BirthDate == null)
            throw Conflict("birth-date-required", $"Informe sua data de nascimento no perfil antes de {action}.");
    }

    private static void EnsureNotSubscribed(User user)
    {
        if (user.SubscriptionId != null)
            throw AlreadySubscribed();
    }

    private static string People(int count) => count == 1 ? "1 pessoa" : $"{count} pessoas";

    private static SubscriptionException AlreadySubscribed() =>
        Conflict("already-subscribed",
            "Você já faz parte de uma assinatura. Cancele o seu plano ou saia do plano atual antes.");

    private static SubscriptionException InviteClosed() =>
        Conflict("invite-closed", "Este convite não está mais disponível.");

    private static SubscriptionException NotFound(string code, string message) =>
        new(SubscriptionErrorKind.NotFound, code, message);

    private static SubscriptionException Forbidden(string code, string message) =>
        new(SubscriptionErrorKind.Forbidden, code, message);

    private static SubscriptionException Conflict(string code, string message) =>
        new(SubscriptionErrorKind.Conflict, code, message);
}

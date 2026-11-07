using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Plans;
using MoviesArchive.Domain.Models.Subscriptions;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Infra.Database;

namespace MoviesArchive.Infra.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly MoviesArchiveDbContext _dbContext;

    public SubscriptionRepository(
        MoviesArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Do menor para o maior: individual antes do familiar
    public Task<List<Plan>> GetActivePlansAsync() =>
        _dbContext.Plans
            .AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.MaxMembers)
            .ThenBy(plan => plan.Price)
            .ToListAsync();

    public Task<Plan?> GetPlanAsync(Guid planId) =>
        _dbContext.Plans.FirstOrDefaultAsync(plan => plan.Id == planId);

    public Task<Subscription?> GetActiveAsync(Guid subscriptionId) =>
        _dbContext.Subscriptions.FirstOrDefaultAsync(subscription =>
            subscription.Id == subscriptionId && subscription.EndedAt == null);

    public Task<List<SubscriptionMember>> GetMembersAsync(Guid subscriptionId) =>
        _dbContext.Users
            .AsNoTracking()
            .Where(user => user.SubscriptionId == subscriptionId)
            .OrderBy(user => user.Name)
            .Select(user => new SubscriptionMember(user.Id, user.Name, user.PhotoData != null, user.BirthDate))
            .ToListAsync();

    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(IReadOnlyCollection<Guid> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.Name })
            .ToDictionaryAsync(user => user.Id, user => user.Name);
    }

    public Task<List<FamilyInvite>> GetValidPendingInvitesAsync(Guid subscriptionId, DateTimeOffset now) =>
        _dbContext.FamilyInvites
            .AsNoTracking()
            .Where(invite =>
                invite.SubscriptionId == subscriptionId &&
                invite.Status == InviteStatus.Pending &&
                invite.ExpiresAt > now)
            .OrderBy(invite => invite.CreatedAt)
            .ToListAsync();

    // Só de assinaturas que continuam ativas
    public Task<List<FamilyInvite>> GetValidReceivedInvitesAsync(Guid userId, DateTimeOffset now) =>
        _dbContext.FamilyInvites
            .AsNoTracking()
            .Where(invite =>
                invite.InvitedUserId == userId &&
                invite.Status == InviteStatus.Pending &&
                invite.ExpiresAt > now &&
                _dbContext.Subscriptions.Any(subscription =>
                    subscription.Id == invite.SubscriptionId && subscription.EndedAt == null))
            .OrderBy(invite => invite.CreatedAt)
            .ToListAsync();

    public Task<FamilyInvite?> GetInviteAsync(Guid inviteId) =>
        _dbContext.FamilyInvites.FirstOrDefaultAsync(invite => invite.Id == inviteId);

    public Task<FamilyInvite?> GetPendingInviteAsync(Guid subscriptionId, Guid invitedUserId) =>
        _dbContext.FamilyInvites.FirstOrDefaultAsync(invite =>
            invite.SubscriptionId == subscriptionId &&
            invite.InvitedUserId == invitedUserId &&
            invite.Status == InviteStatus.Pending);

    public async Task CreateAsync(Subscription subscription, User owner)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // Dois passos: a assinatura existe antes de o titular apontar para ela
        await _dbContext.Subscriptions.AddAsync(subscription);
        await _dbContext.SaveChangesAsync();

        owner.SubscriptionId = subscription.Id;
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task<Subscription> ChangePlanAsync(Subscription current, Guid newPlanId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // Encerrar antes de abrir a nova: o índice só aceita uma assinatura ativa por titular
        current.End(SubscriptionEndReason.PlanChanged);
        await _dbContext.SaveChangesAsync();

        var next = new Subscription(current.OwnerId, newPlanId);
        await _dbContext.Subscriptions.AddAsync(next);
        await _dbContext.SaveChangesAsync();

        // Integrantes e convites pendentes passam para a nova assinatura
        await _dbContext.Users
            .Where(user => user.SubscriptionId == current.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.SubscriptionId, (Guid?)next.Id));
        await _dbContext.FamilyInvites
            .Where(invite => invite.SubscriptionId == current.Id && invite.Status == InviteStatus.Pending)
            .ExecuteUpdateAsync(set => set.SetProperty(invite => invite.SubscriptionId, next.Id));

        await transaction.CommitAsync();

        // O ExecuteUpdate não passa pelo rastreamento do EF: descarta o que ficou desatualizado
        _dbContext.ChangeTracker.Clear();
        return next;
    }

    public async Task EndAsync(Subscription subscription, SubscriptionEndReason reason)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        subscription.End(reason);
        await _dbContext.SaveChangesAsync();

        var now = DateTimeOffset.UtcNow;
        await _dbContext.Users
            .Where(user => user.SubscriptionId == subscription.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(user => user.SubscriptionId, (Guid?)null));
        await _dbContext.FamilyInvites
            .Where(invite => invite.SubscriptionId == subscription.Id && invite.Status == InviteStatus.Pending)
            .ExecuteUpdateAsync(set => set
                .SetProperty(invite => invite.Status, InviteStatus.Canceled)
                .SetProperty(invite => invite.RespondedAt, (DateTimeOffset?)now));

        await transaction.CommitAsync();
        _dbContext.ChangeTracker.Clear();
    }

    public async Task<bool> TryAddInviteAsync(FamilyInvite invite, int maxMembers, FamilyInvite? expiredInviteToCancel)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await LockSubscriptionAsync(invite.SubscriptionId);

        // Um convite vencido ainda ocupa o índice de "um pendente por pessoa": sai antes
        if (expiredInviteToCancel != null)
        {
            expiredInviteToCancel.Respond(InviteStatus.Canceled);
            await _dbContext.SaveChangesAsync();
        }

        var now = DateTimeOffset.UtcNow;
        var taken = await CountMembersAsync(invite.SubscriptionId) +
            await _dbContext.FamilyInvites.CountAsync(item =>
                item.SubscriptionId == invite.SubscriptionId &&
                item.Status == InviteStatus.Pending &&
                item.ExpiresAt > now);

        if (taken >= maxMembers)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _dbContext.FamilyInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<InviteAcceptResult> TryAcceptInviteAsync(FamilyInvite invite, User user, int maxMembers)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        await LockSubscriptionAsync(invite.SubscriptionId);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Users\" WHERE \"Id\" = {user.Id} FOR UPDATE");

        var active = await _dbContext.Subscriptions.AnyAsync(subscription =>
            subscription.Id == invite.SubscriptionId && subscription.EndedAt == null);
        if (!active)
        {
            await transaction.RollbackAsync();
            return InviteAcceptResult.SubscriptionEnded;
        }

        // Confere de novo no banco: a pessoa pode ter assinado outro plano enquanto isso
        var stillFree = await _dbContext.Users.AnyAsync(item => item.Id == user.Id && item.SubscriptionId == null);
        if (!stillFree)
        {
            await transaction.RollbackAsync();
            return InviteAcceptResult.AlreadySubscribed;
        }

        // Os convites pendentes já reservaram as vagas; basta haver lugar entre os integrantes
        if (await CountMembersAsync(invite.SubscriptionId) >= maxMembers)
        {
            await transaction.RollbackAsync();
            return InviteAcceptResult.PlanFull;
        }

        user.SubscriptionId = invite.SubscriptionId;
        invite.Respond(InviteStatus.Accepted);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return InviteAcceptResult.Accepted;
    }

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();

    private Task<int> CountMembersAsync(Guid subscriptionId) =>
        _dbContext.Users.CountAsync(user => user.SubscriptionId == subscriptionId);

    // Trava a linha da assinatura até o fim da transação
    private Task LockSubscriptionAsync(Guid subscriptionId) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Subscriptions\" WHERE \"Id\" = {subscriptionId} FOR UPDATE");
}

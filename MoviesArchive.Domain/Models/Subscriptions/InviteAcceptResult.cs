namespace MoviesArchive.Domain.Models.Subscriptions;

// Resultado de aceitar um convite, conferido com a assinatura travada no banco
public enum InviteAcceptResult
{
    Accepted,
    PlanFull,
    SubscriptionEnded,
    AlreadySubscribed,
}

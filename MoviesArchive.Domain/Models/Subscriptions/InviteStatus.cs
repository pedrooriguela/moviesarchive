namespace MoviesArchive.Domain.Models.Subscriptions;

// Gravado como texto em FamilyInvites.Status.
// Expirado não é um status: é um Pending com ExpiresAt no passado.
public enum InviteStatus
{
    Pending,
    Accepted,
    Declined,
    Canceled,
}

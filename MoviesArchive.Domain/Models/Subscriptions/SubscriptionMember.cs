using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Models.Subscriptions;

// Integrante do jeito que a tela precisa, sem carregar a foto inteira do banco
public record SubscriptionMember(Guid UserId, string Name, bool HasPhoto, DateOnly? BirthDate)
{
    public bool IsMinorOn(DateOnly date) =>
        BirthDate is DateOnly birthDate && BirthDates.AgeOn(birthDate, date) < User.AdultAge;
}

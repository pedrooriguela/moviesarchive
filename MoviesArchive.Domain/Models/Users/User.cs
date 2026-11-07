using System.ComponentModel.DataAnnotations;
using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Users;

public class User : Entity
{
    public User(
        string name,
        string email)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; set; }
    public string Email { get; set; }
    public byte[]? PhotoData { get; set; }
    public string? PhotoContentType { get; set; }
    public string? Password { get; set; }
    public string? PasswordResetToken  { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    // Obrigatória no cadastro novo. Contas antigas não têm: preenchem antes de contratar
    // um plano ou aceitar um convite, então ninguém sem data chega a ter acesso aos filmes.
    public DateOnly? BirthDate { get; set; }

    // Assinatura ATIVA de que a pessoa faz parte, como titular ou convidada.
    // Nula = conta gratuita. Ao encerrar a assinatura, volta a ser nula para todos.
    public Guid? SubscriptionId { get; set; }

    public const int AdultAge = 18;

    // Idade em anos completos na data informada; nula sem data de nascimento
    public int? AgeOn(DateOnly date) =>
        BirthDate is DateOnly birthDate ? BirthDates.AgeOn(birthDate, date) : null;

    public bool IsAdultOn(DateOnly date) => AgeOn(date) >= AdultAge;

    // Maior classificação indicativa liberada: a idade, para menores; nula (sem limite) para os demais
    public int? MaxAgeRatingOn(DateOnly date) =>
        AgeOn(date) is int age && age < AdultAge ? age : null;


    public void SetPassword (string password) =>
        Password = password;

    public class Builder
    {
        public static User Create(string name, string email) =>
            new(name, email);
    }
}

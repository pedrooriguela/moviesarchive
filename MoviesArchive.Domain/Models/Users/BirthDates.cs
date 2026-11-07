namespace MoviesArchive.Domain.Models.Users;

// Regras de data de nascimento e idade, usadas no cadastro, no perfil e nas assinaturas
public static class BirthDates
{
    public static readonly DateOnly MinDate = new(1900, 1, 1);

    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    // Nem antes de 1900 nem no futuro
    public static bool IsValid(DateOnly birthDate, DateOnly today) =>
        birthDate >= MinDate && birthDate <= today;

    // Anos completos na data informada
    public static int AgeOn(DateOnly birthDate, DateOnly date)
    {
        var age = date.Year - birthDate.Year;
        if (birthDate.AddYears(age) > date)
            age--;

        return age;
    }
}

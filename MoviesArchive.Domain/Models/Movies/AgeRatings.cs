namespace MoviesArchive.Domain.Models.Movies;

// Classificação indicativa no padrão brasileiro, guardada como idade mínima em Movies.AgeRating.
// Para mudar a lista, altere aqui e a CHECK CK_Movies_AgeRating do schema.
public static class AgeRatings
{
    public const int Free = 0;
    public const int Adult = 18;

    public static readonly IReadOnlyList<int> All = [0, 10, 12, 14, 16, 18];

    public static bool IsValid(int rating) => All.Contains(rating);

    // "L" para livre; os demais pela idade
    public static string ToDisplayName(int rating) =>
        rating == Free ? "L" : rating.ToString();

    // Filme liberado para alguém dessa idade
    public static bool AllowsAge(int rating, int age) => rating <= age;
}

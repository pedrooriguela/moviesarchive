namespace MoviesArchive.Domain.Models.Movies;

// Gêneros aceitos. O nome de cada membro é o valor gravado em Movies.Genres (text[]).
// Para mudar a lista, altere este enum e a CHECK CK_Movies_Genres do schema.
public enum Genre
{
    Acao,
    Comedia,
    Crime,
    Drama,
    Fantasia,
    FiccaoCientifica,
    Romance,
    Suspense,
    Terror,
}

public static class GenreExtensions
{
    // Nome exibido ao usuário
    public static string ToDisplayName(this Genre genre) => genre switch
    {
        Genre.Acao => "Ação",
        Genre.Comedia => "Comédia",
        Genre.Crime => "Crime",
        Genre.Drama => "Drama",
        Genre.Fantasia => "Fantasia",
        Genre.FiccaoCientifica => "Ficção científica",
        Genre.Romance => "Romance",
        Genre.Suspense => "Suspense",
        Genre.Terror => "Terror",
        _ => genre.ToString(),
    };
}

using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Movies;

public class Movie : Entity
{
    public Movie(
        string title,
        DateOnly? releaseDate)
    {
        Title = title;
        ReleaseDate = releaseDate;
    }

    public string Title { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
    public string? PosterUrl { get; set; }
    public string? Director { get; set; }

    // Nomes do enum Genre, gravados em text[]
    public List<string> Genres { get; set; } = [];

    // Classificação indicativa: idade mínima (0 = livre). Ver AgeRatings.
    // Sem classificação conhecida, 18: na dúvida, esconder de menores.
    public int AgeRating { get; set; } = AgeRatings.Adult;

    public int? RuntimeMinutes { get; set; }

    public class Builder
    {
        public static Movie Create(string title, DateOnly? releaseDate) =>
            new(title, releaseDate);
    }
}

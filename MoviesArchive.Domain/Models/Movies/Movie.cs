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
    public class Builder
    {
        public static Movie Create(string title, DateOnly? releaseDate) =>
            new(title, releaseDate);
    }
}
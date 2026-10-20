namespace MoviesArchive.Domain.Models.Movies.Dtos;

public class MovieResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
}

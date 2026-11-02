namespace MoviesArchive.Domain.Models.Movies.Dtos;

// Ficha completa do filme
public class MovieDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public string? Language { get; set; }
    public string? Director { get; set; }
    public List<string> Genres { get; set; } = [];
    public decimal? Rating { get; set; }
    public int Votes { get; set; }
    public string? Synopsis { get; set; }
    public string? PosterUrl { get; set; }
}

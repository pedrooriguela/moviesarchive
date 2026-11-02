namespace MoviesArchive.Domain.Models.Movies.Dtos;

// Página inicial: destaque e prateleiras
public class MovieHomeDto
{
    public MovieDetailDto? Featured { get; set; }
    public List<MovieShelfDto> Shelves { get; set; } = [];
}

public class MovieShelfDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<MovieCardDto> Movies { get; set; } = [];
}

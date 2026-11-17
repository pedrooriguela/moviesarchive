namespace MoviesArchive.Domain.Models.Admin.Dtos;

// Filme como o administrador vê: todos os campos editáveis, inclusive o vídeo, e se está removido
public class AdminMovieDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public int? ReleaseYear { get; set; }
    public string? Description { get; set; }
    public string? Language { get; set; }
    public string? Director { get; set; }
    public string? PosterUrl { get; set; }

    // Códigos do enum Genre (ex.: "Acao"), como no formulário e no banco
    public List<string> Genres { get; set; } = [];
    public int AgeRating { get; set; }
    public int? RuntimeMinutes { get; set; }
    public string? VideoUrl { get; set; }

    public bool IsRemoved { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Quantas vezes o filme foi assistido (cliques no play), desde sempre
    public int AccessCount { get; set; }
}

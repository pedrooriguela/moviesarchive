namespace MoviesArchive.Domain.Models.History.Dtos;

// Item da linha do tempo: quando o filme foi assistido e o resumo do filme
public class WatchHistoryEntryResponseDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public string? PosterUrl { get; set; }
    public DateTimeOffset WatchedAt { get; set; }
}

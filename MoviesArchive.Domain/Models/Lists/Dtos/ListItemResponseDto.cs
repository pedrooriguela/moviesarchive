namespace MoviesArchive.Domain.Models.Lists.Dtos;

public class ListItemResponseDto
{
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}

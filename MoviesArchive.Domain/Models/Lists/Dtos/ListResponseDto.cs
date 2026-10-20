namespace MoviesArchive.Domain.Models.Lists.Dtos;

public class ListResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ListItemResponseDto> Items { get; set; } = new();
}

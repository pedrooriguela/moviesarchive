namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

public class PlanResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty; // "Individual" ou "Family"
    public decimal Price { get; set; }
    public int MaxMembers { get; set; }
}

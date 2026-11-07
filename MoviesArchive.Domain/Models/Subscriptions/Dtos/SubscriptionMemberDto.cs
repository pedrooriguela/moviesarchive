namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

public class SubscriptionMemberDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsOwner { get; set; }
    public bool IsMinor { get; set; }
}

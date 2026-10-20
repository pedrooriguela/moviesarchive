namespace MoviesArchive.Domain.Models.Users.Dtos;

public class UserProfileResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}
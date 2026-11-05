namespace MoviesArchive.Domain.Models.Users.Dtos;

// Somente informações públicas; nunca serializar a entidade User na busca.
public class SocialUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public bool IsFollowing { get; set; }
}

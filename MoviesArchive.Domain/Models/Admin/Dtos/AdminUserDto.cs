namespace MoviesArchive.Domain.Models.Admin.Dtos;

// Usuário na aba "Administradores" do painel
public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsAdmin { get; set; }

    // A própria pessoa logada: não pode tirar o próprio acesso
    public bool IsMe { get; set; }
}

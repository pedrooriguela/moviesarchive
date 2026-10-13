using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Users.Dtos;

public class UpdateUserProfileRequestDto
{
    [Required(ErrorMessage = "Nome de usuário é obrigatório")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "O nome deve ter até 50 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Url(ErrorMessage = "Informe uma URL válida para a foto")]
    public string? PhotoUrl { get; set; }
}
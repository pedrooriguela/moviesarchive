using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

public class FamilyInviteRequestDto
{
    [Required(ErrorMessage = "Informe o nome de usuário")]
    [StringLength(50, ErrorMessage = "O nome deve ter até 50 caracteres")]
    public string UserName { get; set; } = string.Empty;
}

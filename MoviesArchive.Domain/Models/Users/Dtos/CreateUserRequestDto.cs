using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MoviesArchive.Domain.Models.Users.Dtos;

public class CreateUserRequestDto
{
    [Required(ErrorMessage = "Nome de usuário é obrigatório")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório")]
    [EmailAddress(ErrorMessage = "Insira um email de formato válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    // Anulável para que a ausência do campo seja recusada pelo [Required]
    [Required(ErrorMessage = "A data de nascimento é obrigatória")]
    public DateOnly? BirthDate { get; set; }
}

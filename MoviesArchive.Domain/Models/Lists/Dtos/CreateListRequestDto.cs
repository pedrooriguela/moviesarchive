using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Lists.Dtos;

public class CreateListRequestDto
{
    [Required(ErrorMessage = "O título da lista é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "O título deve ter até 100 caracteres")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "A descrição deve ter até 500 caracteres")]
    public string? Description { get; set; }

    public bool IsPublic { get; set; }
}

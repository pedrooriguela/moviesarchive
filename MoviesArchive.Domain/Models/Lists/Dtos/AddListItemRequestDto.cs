using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Lists.Dtos;

public class AddListItemRequestDto
{
    [Required(ErrorMessage = "O filme é obrigatório")]
    public Guid MovieId { get; set; }
}

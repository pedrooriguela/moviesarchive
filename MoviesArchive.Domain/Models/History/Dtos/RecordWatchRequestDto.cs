using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.History.Dtos;

public class RecordWatchRequestDto
{
    [Required(ErrorMessage = "O filme é obrigatório")]
    public Guid MovieId { get; set; }
}

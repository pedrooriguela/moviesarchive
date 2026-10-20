using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Lists.Dtos;

public class UpdateListItemsOrderRequestDto
{
    // Ids de todos os filmes da lista, na nova ordem desejada
    [Required(ErrorMessage = "A nova ordem dos filmes é obrigatória")]
    public List<Guid> MovieIds { get; set; } = new();
}

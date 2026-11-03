using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Users.Dtos;

public class DeleteAccountRequestDto
{
    [Required(ErrorMessage = "Informe sua senha para confirmar")]
    public string Password { get; set; } = string.Empty;

    // Anulável para que a ausência do campo seja recusada, em vez de assumir um destino
    [Required(ErrorMessage = "Escolha o destino do seu conteúdo")]
    public ContentDestination? Destination { get; set; }
}

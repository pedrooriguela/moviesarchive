namespace MoviesArchive.Domain.Models.Reviews.Dtos;

// Cria ou substitui a avaliação do usuário logado. A nota é validada pela entidade Review.
public class UpsertReviewRequestDto
{
    // De 0,5 a 5, em passos de 0,5
    public decimal Rating { get; set; }

    // Opcional, até 2000 caracteres
    public string? Comment { get; set; }
}

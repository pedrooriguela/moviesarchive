namespace MoviesArchive.Domain.Models.Reviews.Dtos;

public class CreateReplyRequestDto
{
    // Até 1000 caracteres
    public string Comment { get; set; } = string.Empty;

    // Preenchido quando é resposta a outra resposta da mesma avaliação
    public Guid? ParentReplyId { get; set; }
}

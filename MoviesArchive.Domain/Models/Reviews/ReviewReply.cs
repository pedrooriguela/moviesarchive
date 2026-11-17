using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Exceptions;

namespace MoviesArchive.Domain.Models.Reviews;

// Resposta a uma avaliação (tabela "Responses"). Com ParentReplyId, é resposta a outra resposta.
public class ReviewReply : Entity
{
    public const int MaxCommentLength = 1000;

    // Também é o construtor que o EF usa ao ler do banco: por isso não valida nada aqui
    // (uma resposta excluída volta com o texto vazio). A validação fica no Builder.Create.
    public ReviewReply(Guid reviewId, Guid userId, Guid? parentReplyId, string comment)
    {
        ReviewId = reviewId;
        UserId = userId;
        ParentReplyId = parentReplyId;
        Comment = comment;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentReplyId { get; set; }
    public string Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Excluída mas mantida porque outras respostas dependem dela: o texto some, a conversa continua
    public DateTimeOffset? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt != null;

    public void MarkAsDeleted()
    {
        Comment = string.Empty;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    private static string NormalizeComment(string comment)
    {
        var trimmed = comment?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new InvalidReplyException("Escreva a resposta antes de enviar.");
        if (trimmed.Length > MaxCommentLength)
            throw new InvalidReplyException($"A resposta deve ter até {MaxCommentLength} caracteres.");
        return trimmed;
    }

    public class Builder
    {
        // Resposta nova: texto obrigatório, sem espaços nas pontas, até MaxCommentLength
        public static ReviewReply Create(Guid reviewId, Guid userId, Guid? parentReplyId, string comment) =>
            new(reviewId, userId, parentReplyId, NormalizeComment(comment));
    }
}

namespace MoviesArchive.Domain.Exceptions;

public class ReplyForbiddenException : Exception
{
    public ReplyForbiddenException() : base("Só quem escreveu a resposta pode excluí-la.") { }
}

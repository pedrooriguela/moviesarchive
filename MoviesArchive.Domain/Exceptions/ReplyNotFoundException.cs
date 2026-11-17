namespace MoviesArchive.Domain.Exceptions;

public class ReplyNotFoundException : Exception
{
    public ReplyNotFoundException() : base("Resposta não encontrada.") { }
}

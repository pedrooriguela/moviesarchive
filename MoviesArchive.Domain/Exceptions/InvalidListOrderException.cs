namespace MoviesArchive.Domain.Exceptions;

public class InvalidListOrderException : Exception
{
    public InvalidListOrderException() : base("A nova ordem deve conter exatamente os filmes da lista, sem repetições") { }
}

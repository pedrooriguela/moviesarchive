namespace MoviesArchive.Domain.Exceptions;

public class MovieAlreadyExistsException : Exception
{
    public MovieAlreadyExistsException() : base("Já existe um filme com esse título e esse ano no catálogo") { }
}

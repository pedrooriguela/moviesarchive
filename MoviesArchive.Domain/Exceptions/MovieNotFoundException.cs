namespace MoviesArchive.Domain.Exceptions;

public class MovieNotFoundException : Exception
{
    public MovieNotFoundException() : base("Filme não encontrado") { }
}

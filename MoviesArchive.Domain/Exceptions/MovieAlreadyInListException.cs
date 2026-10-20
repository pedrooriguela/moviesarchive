namespace MoviesArchive.Domain.Exceptions;

public class MovieAlreadyInListException : Exception
{
    public MovieAlreadyInListException() : base("Este filme já está na lista") { }
}

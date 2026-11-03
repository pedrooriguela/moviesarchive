namespace MoviesArchive.Domain.Exceptions;

public class ReviewNotFoundException : Exception
{
    public ReviewNotFoundException() : base("Você ainda não avaliou este filme.") { }
}

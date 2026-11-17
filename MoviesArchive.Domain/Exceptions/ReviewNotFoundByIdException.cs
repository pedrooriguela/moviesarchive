namespace MoviesArchive.Domain.Exceptions;

public class ReviewNotFoundByIdException : Exception
{
    public ReviewNotFoundByIdException() : base("Avaliação não encontrada.") { }
}

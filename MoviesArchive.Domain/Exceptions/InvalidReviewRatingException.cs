namespace MoviesArchive.Domain.Exceptions;

public class InvalidReviewRatingException : Exception
{
    public InvalidReviewRatingException() : base("A nota deve ficar entre 0,5 e 5 estrelas, em passos de meia estrela.") { }
}

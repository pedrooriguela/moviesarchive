namespace MoviesArchive.Domain.Exceptions;

public class ReviewCommentTooLongException : Exception
{
    public ReviewCommentTooLongException(int maxLength) : base($"O comentário deve ter até {maxLength} caracteres.") { }
}

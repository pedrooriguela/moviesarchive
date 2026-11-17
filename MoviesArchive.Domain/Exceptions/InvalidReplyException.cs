namespace MoviesArchive.Domain.Exceptions;

public class InvalidReplyException : Exception
{
    public InvalidReplyException(string message) : base(message) { }
}

namespace MoviesArchive.Domain.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException() : base("Sua sessão expirou. Entre de novo.") { }
}

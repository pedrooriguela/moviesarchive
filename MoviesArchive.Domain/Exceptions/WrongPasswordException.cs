namespace MoviesArchive.Domain.Exceptions;

public class WrongPasswordException : Exception
{
    public WrongPasswordException() : base("A senha informada está incorreta") { }
}

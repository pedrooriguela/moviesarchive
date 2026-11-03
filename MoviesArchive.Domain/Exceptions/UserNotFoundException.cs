namespace MoviesArchive.Domain.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException() : base("Usuário não encontrado") { }
}

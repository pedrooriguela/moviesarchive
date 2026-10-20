namespace MoviesArchive.Domain.Exceptions;

public class ListTitleAlreadyInUseException : Exception
{
    public ListTitleAlreadyInUseException() : base("Você já possui uma lista com este título") { }
}

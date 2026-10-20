namespace MoviesArchive.Domain.Exceptions;

public class ListNotFoundException : Exception
{
    public ListNotFoundException() : base("Lista não encontrada") { }
}

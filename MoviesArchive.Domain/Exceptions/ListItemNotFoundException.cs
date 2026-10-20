namespace MoviesArchive.Domain.Exceptions;

public class ListItemNotFoundException : Exception
{
    public ListItemNotFoundException() : base("Este filme não está na lista") { }
}

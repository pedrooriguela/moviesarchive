namespace MoviesArchive.Domain.Exceptions;

public class ReservedListTitleException : Exception
{
    public ReservedListTitleException() : base("O nome \"Watchlist\" é reservado para a sua watchlist") { }
}

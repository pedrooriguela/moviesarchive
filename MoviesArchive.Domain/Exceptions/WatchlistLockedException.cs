namespace MoviesArchive.Domain.Exceptions;

public class WatchlistLockedException : Exception
{
    public WatchlistLockedException() : base("A watchlist não pode ser renomeada nem excluída") { }
}

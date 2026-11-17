namespace MoviesArchive.Domain.Models.Recommendations;

// O que o banco sabe do gosto de uma pessoa: é a entrada do motor de recomendação
public record ViewerSignals(
    IReadOnlyDictionary<Guid, int> WatchCounts,  // filme -> quantas vezes a pessoa deu play (WatchHistory)
    IReadOnlyDictionary<Guid, decimal> Ratings,  // filme -> nota que a pessoa deu, de 0,5 a 5 (Reviews)
    IReadOnlySet<Guid> Watchlist)                // filmes guardados para ver depois (Watchlist)
{
    public bool IsEmpty => WatchCounts.Count == 0 && Ratings.Count == 0 && Watchlist.Count == 0;

    // Já assistiu, avaliou ou guardou: não faz sentido recomendar de novo
    public bool HasInteracted(Guid movieId) =>
        WatchCounts.ContainsKey(movieId) || Ratings.ContainsKey(movieId) || Watchlist.Contains(movieId);
}

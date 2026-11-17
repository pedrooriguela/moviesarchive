namespace MoviesArchive.Domain.Exceptions;

// Conta gratuita tentando assistir: o plano é o que libera os filmes
public class SubscriptionRequiredException : Exception
{
    public SubscriptionRequiredException() : base("Assine um plano para assistir aos filmes.") { }
}

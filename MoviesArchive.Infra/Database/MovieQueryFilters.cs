namespace MoviesArchive.Infra.Database;

// Nomes dos filtros globais de filmes (ver MoviesArchiveDbContext). Com nome, uma consulta
// pode desligar só um deles: o histórico mostra filmes removidos, mas continua respeitando a idade.
public static class MovieQueryFilters
{
    // Esconde filmes acima da classificação indicativa de quem está pedindo
    public const string AgeRating = "AgeRating";

    // Esconde filmes removidos do catálogo pelo administrador (DeletedAt preenchido)
    public const string Removed = "Removed";
}

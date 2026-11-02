namespace MoviesArchive.Domain.Models.Movies;

// Critérios da busca do catálogo. Valores já validados pelo controller.
public record MovieSearchFilter(
    string? Query,
    string? Language,
    int? YearFrom,
    int? YearTo,
    string Sort,
    int Page,
    int PageSize);

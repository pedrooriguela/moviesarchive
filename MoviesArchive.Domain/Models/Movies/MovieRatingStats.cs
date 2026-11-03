namespace MoviesArchive.Domain.Models.Movies;

// Média e quantidade de avaliações de um filme, calculadas a partir da tabela Reviews
public record MovieRatingStats(decimal AverageRating, int ReviewCount);

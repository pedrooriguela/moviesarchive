using MoviesArchive.Domain.Models.Movies;

namespace MoviesArchive.Domain.Models.Recommendations;

// Um filme recomendado, a pontuação que ele teve e o motivo mostrado no cartão
public record Recommendation(Movie Movie, double Score, string Reason);

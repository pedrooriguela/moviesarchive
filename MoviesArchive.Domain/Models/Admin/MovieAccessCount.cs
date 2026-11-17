namespace MoviesArchive.Domain.Models.Admin;

// Acessos (cliques no play) de um filme e quantas pessoas diferentes assistiram
public record MovieAccessCount(Guid MovieId, int Count, int Viewers);

namespace MoviesArchive.Domain.Models.Reviews;

// Avaliação junto com os dados do autor que aparecem na página do filme
public record ReviewWithAuthor(Review Review, string AuthorName, bool AuthorHasPhoto);

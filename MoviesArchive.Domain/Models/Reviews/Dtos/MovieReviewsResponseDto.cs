namespace MoviesArchive.Domain.Models.Reviews.Dtos;

// Página de avaliações de um filme, com o resumo das notas de todos os espectadores
public class MovieReviewsResponseDto
{
    public List<ReviewResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    // Média das notas (null quando ainda não há avaliações)
    public decimal? AverageRating { get; set; }

    // Quantidade de avaliações por nota: índice 0 = 0,5 estrela ... índice 9 = 5 estrelas
    public int[] Histogram { get; set; } = new int[10];
}

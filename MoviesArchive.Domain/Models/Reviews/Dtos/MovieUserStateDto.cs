namespace MoviesArchive.Domain.Models.Reviews.Dtos;

// O que o usuário logado já fez com o filme
public class MovieUserStateDto
{
    public bool InWatchlist { get; set; }
    public ReviewResponseDto? Review { get; set; }
}

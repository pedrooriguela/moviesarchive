namespace MoviesArchive.Domain.Models.Reviews.Dtos;

// Resultado de curtir ou descurtir
public class LikeStateDto
{
    public int LikeCount { get; set; }
    public bool LikedByMe { get; set; }
}

namespace MoviesArchive.Domain.Models.Reviews;

// Curtidas e respostas de uma avaliação, do ponto de vista de quem está vendo
public record ReviewEngagement(int LikeCount, bool LikedByViewer, int ReplyCount);

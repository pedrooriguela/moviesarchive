namespace MoviesArchive.Domain.Models.Reviews;

// Resposta com o autor e as curtidas, como aparece na conversa
public record ReplyWithAuthor(ReviewReply Reply, string AuthorName, bool AuthorHasPhoto, int LikeCount, bool LikedByViewer);

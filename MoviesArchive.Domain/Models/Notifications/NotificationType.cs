using System.Text.Json.Serialization;

namespace MoviesArchive.Domain.Models.Notifications;

// O que aconteceu com algo que o usuário escreveu
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    // Alguém curtiu a avaliação do usuário
    ReviewLiked,

    // Alguém curtiu uma resposta do usuário
    ReplyLiked,

    // Alguém respondeu à avaliação do usuário
    ReviewReplied,

    // Alguém respondeu a uma resposta do usuário
    ReplyReplied
}

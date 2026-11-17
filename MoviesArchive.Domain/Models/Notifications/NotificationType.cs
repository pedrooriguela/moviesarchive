using System.Text.Json.Serialization;

namespace MoviesArchive.Domain.Models.Notifications;

// O que aconteceu: com algo que o usuário escreveu, ou com um convite do plano familiar
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
    ReplyReplied,

    // O titular de um plano familiar convidou o usuário
    PlanInviteReceived,

    // Quem o usuário (titular) convidou aceitou o convite
    PlanInviteAccepted,

    // Quem o usuário (titular) convidou recusou o convite
    PlanInviteDeclined
}

public static class NotificationTypeExtensions
{
    public static bool IsAboutPlan(this NotificationType type) =>
        type is NotificationType.PlanInviteReceived
            or NotificationType.PlanInviteAccepted
            or NotificationType.PlanInviteDeclined;
}

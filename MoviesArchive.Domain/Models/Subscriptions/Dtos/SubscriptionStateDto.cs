namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

// Tudo que a tela de assinatura precisa. Toda ação devolve este objeto atualizado.
public class SubscriptionStateDto
{
    // Nula = conta gratuita
    public SubscriptionResponseDto? Subscription { get; set; }
    public List<ReceivedInviteDto> ReceivedInvites { get; set; } = [];
    public bool HasBirthDate { get; set; }
    public bool IsAdult { get; set; }

    // Sem assinatura e maior de idade
    public bool CanSubscribe { get; set; }
}

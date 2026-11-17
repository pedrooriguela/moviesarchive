using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Subscriptions.Dtos;

// Contratar ou trocar de plano
public class SubscribeRequestDto
{
    [Required(ErrorMessage = "Escolha um plano")]
    public Guid? PlanId { get; set; }
}

using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Plans;

// Plano à venda. A conta gratuita não é um plano: é a conta sem assinatura.
public class Plan : Entity
{
    // O familiar comporta no máximo 4 integrantes, titular incluído (requisito)
    public const int MaxFamilyMembers = 4;

    public Plan(string name, PlanType type, decimal price, int maxMembers, string? description)
    {
        Name = name;
        Type = type;
        Price = price;
        MaxMembers = maxMembers;
        Description = description;
        IsActive = true;
    }

    public string Name { get; set; }
    public string? Description { get; set; }
    public PlanType Type { get; set; }

    // Preço mensal atual; vale para todos os assinantes do plano
    public decimal Price { get; set; }

    // 1 no individual, até 4 no familiar
    public int MaxMembers { get; set; }

    // Fora de venda: ninguém novo contrata, quem já assina continua
    public bool IsActive { get; set; }
}

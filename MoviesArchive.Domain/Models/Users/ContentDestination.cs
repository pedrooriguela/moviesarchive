using System.Text.Json.Serialization;

namespace MoviesArchive.Domain.Models.Users;

/// <summary>O que acontece com o conteúdo de quem exclui a conta.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ContentDestination
{
    /// <summary>Avaliações, respostas e listas públicas ficam, assinadas como usuário removido.</summary>
    Anonymize,

    /// <summary>Tudo que pertence à conta é apagado junto com ela.</summary>
    Delete
}

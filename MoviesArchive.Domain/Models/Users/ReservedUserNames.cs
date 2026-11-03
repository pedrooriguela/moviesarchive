namespace MoviesArchive.Domain.Models.Users;

/// <summary>Nomes que nenhuma pessoa pode escolher, para ninguém se passar por conta removida.</summary>
public static class ReservedUserNames
{
    public const string Removed = "Usuário removido";

    public static bool IsReserved(string name) =>
        string.Equals(name.Trim(), Removed, StringComparison.OrdinalIgnoreCase);
}

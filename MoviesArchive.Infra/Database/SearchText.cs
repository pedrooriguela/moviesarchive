using System.Text;

namespace MoviesArchive.Infra.Database;

// Busca sem diferenciar acentos nem maiúsculas, com a mesma regra nos dois lados da comparação:
// no banco, translate(lower(coluna), Accented, Plain); aqui no C#, Normalize(texto).
// Assim "toquio", "Tóquio" e "TÓQUIO" se encontram. Não depende da extensão unaccent do PostgreSQL.
public static class SearchText
{
    // As maiúsculas também entram: em bancos com COLLATE "C", o lower() não converte letras acentuadas
    private static readonly (string Letters, char Plain)[] Groups =
    [
        ("àáâãäåāăąÀÁÂÃÄÅĀĂĄ", 'a'),
        ("èéêëēĕėęěÈÉÊËĒĔĖĘĚ", 'e'),
        ("ìíîïĩīĭįıÌÍÎÏĨĪĬĮİ", 'i'),
        ("òóôõöøōŏőÒÓÔÕÖØŌŎŐ", 'o'),
        ("ùúûüũūŭůűųÙÚÛÜŨŪŬŮŰŲ", 'u'),
        ("ýÿÝŸ", 'y'),
        ("çćčÇĆČ", 'c'),
        ("ñńňÑŃŇ", 'n'),
        ("śšŚŠ", 's'),
        ("źżžŹŻŽ", 'z'),
        ("łŁ", 'l'),
        ("đĐ", 'd'),
        ("řŘ", 'r'),
        ("ğĞ", 'g'),
    ];

    // Letras com acento e, na mesma posição, a letra sem acento (argumentos do translate)
    public static readonly string Accented = string.Concat(Groups.Select(group => group.Letters));
    public static readonly string Plain = string.Concat(Groups.Select(group => new string(group.Plain, group.Letters.Length)));

    private static readonly Dictionary<char, char> Map = Groups
        .SelectMany(group => group.Letters.Select(letter => (Letter: letter, group.Plain)))
        .ToDictionary(pair => pair.Letter, pair => pair.Plain);

    // Vira a função translate() do PostgreSQL (registrada no MoviesArchiveDbContext).
    // Só funciona dentro de consultas do EF.
    public static string Translate(string text, string from, string to) =>
        throw new NotSupportedException("SearchText.Translate só pode ser usada em consultas do EF.");

    // Mesmo resultado de translate(lower(texto), Accented, Plain), feito em C#
    public static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text.ToLowerInvariant())
            builder.Append(Map.TryGetValue(character, out var plain) ? plain : character);

        return builder.ToString();
    }

    // Palavras da busca, já normalizadas: "  Kubrick   Iluminado " -> ["kubrick", "iluminado"].
    // O limite evita consultas enormes com textos longos.
    public static List<string> Terms(string? text, int max = 6) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : Normalize(text)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Distinct()
                .Take(max)
                .ToList();
}

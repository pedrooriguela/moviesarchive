using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace MoviesArchive.Infra.Settings;

public class JwtSettings
{
    public string Key { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }

    // Vida do JWT enviado em cada requisição; curto porque não dá para revogá-lo
    public int AccessTokenMinutes { get; set; } = DefaultAccessTokenMinutes;

    // Vida do refresh token (cookie httpOnly), renovada a cada uso
    public int RefreshTokenDays { get; set; } = DefaultRefreshTokenDays;

    public const int DefaultAccessTokenMinutes = 15;
    public const int DefaultRefreshTokenDays = 7;
}

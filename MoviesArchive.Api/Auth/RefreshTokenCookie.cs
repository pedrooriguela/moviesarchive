namespace MoviesArchive.Api.Auth;

// O refresh token mora num cookie httpOnly: o JavaScript da página não consegue lê-lo,
// então um script injetado não rouba a sessão longa. Ele só é enviado para /api/auth.
public static class RefreshTokenCookie
{
    public const string Name = "moviesarchive_refresh";
    private const string Path = "/api/auth";

    public static void Write(HttpResponse response, string refreshToken, DateTimeOffset expiresAt)
    {
        response.Cookies.Append(Name, refreshToken, BuildOptions(response.HttpContext.Request, expiresAt));
    }

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) ? value : null;

    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(Name, BuildOptions(response.HttpContext.Request, null));
    }

    // SameSite=Strict: o front (localhost:5173) e a API (localhost:5197) são o mesmo site,
    // então o cookie vai junto; nenhum outro site consegue fazer o navegador enviá-lo.
    // Secure só em HTTPS, para continuar funcionando no http://localhost do desenvolvimento.
    private static CookieOptions BuildOptions(HttpRequest request, DateTimeOffset? expiresAt) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresAt,
        IsEssential = true,
    };
}

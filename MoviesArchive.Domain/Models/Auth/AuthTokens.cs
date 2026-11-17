namespace MoviesArchive.Domain.Models.Auth;

// Resultado do login e da renovação: o JWT curto vai no corpo da resposta,
// o refresh token vai num cookie httpOnly
public record AuthTokens(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

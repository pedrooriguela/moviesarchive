using MoviesArchive.Domain.Models.Auth;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface IAuthTokenService
{
    // Login: novo JWT + novo refresh token
    Task<AuthTokens> IssueAsync(User user);

    // Troca o refresh token por um novo (rotação) e devolve um JWT novo
    Task<AuthTokens> RefreshAsync(string refreshToken);

    // Logout: encerra a sessão deste refresh token
    Task RevokeAsync(string refreshToken);

    Task RevokeAllAsync(Guid userId);
}

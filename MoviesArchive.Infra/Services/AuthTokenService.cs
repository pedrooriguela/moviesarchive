using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Auth;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Infra.Settings;

namespace MoviesArchive.Infra.Services;

// Sessões: JWT de vida curta + refresh token de vida longa, trocado a cada uso (rotação).
// Se um refresh token já trocado aparecer de novo, alguém pode tê-lo copiado:
// todas as sessões do usuário são encerradas.
public class AuthTokenService : IAuthTokenService
{
    // Duas abas renovando ao mesmo tempo mandam o mesmo token; a segunda não deve derrubar tudo
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly int _refreshTokenDays;

    public AuthTokenService(
        IJwtService jwtService,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _jwtService = jwtService;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;

        var settings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
        _refreshTokenDays = settings?.RefreshTokenDays is > 0 ? settings.RefreshTokenDays : JwtSettings.DefaultRefreshTokenDays;
    }

    public async Task<AuthTokens> IssueAsync(User user)
    {
        await _refreshTokenRepository.DeleteStaleForUserAsync(user.Id);

        var (rawToken, record) = CreateRefreshToken(user.Id);
        await _refreshTokenRepository.AddAsync(record);

        return new AuthTokens(_jwtService.GenerateToken(user), rawToken, record.ExpiresAt);
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken)
    {
        var now = DateTimeOffset.UtcNow;
        var current = string.IsNullOrWhiteSpace(refreshToken)
            ? null
            : await _refreshTokenRepository.GetByHashAsync(Hash(refreshToken));

        if (current == null)
            throw new InvalidRefreshTokenException();

        if (!current.IsActive(now))
        {
            // Reuso de um token já trocado (fora da janela de duas abas): encerra todas as sessões
            if (current.ReplacedByTokenHash != null && current.RevokedAt < now - ReuseGracePeriod)
                await _refreshTokenRepository.RevokeAllForUserAsync(current.UserId);

            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.GetByIdAsync(current.UserId)
            ?? throw new InvalidRefreshTokenException();

        var (rawToken, next) = CreateRefreshToken(user.Id);
        current.Revoke(now, next.TokenHash);
        await _refreshTokenRepository.UpdateAsync(current);
        await _refreshTokenRepository.AddAsync(next);

        return new AuthTokens(_jwtService.GenerateToken(user), rawToken, next.ExpiresAt);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var token = await _refreshTokenRepository.GetByHashAsync(Hash(refreshToken));
        if (token == null || token.RevokedAt != null)
            return;

        token.Revoke(DateTimeOffset.UtcNow);
        await _refreshTokenRepository.UpdateAsync(token);
    }

    public Task RevokeAllAsync(Guid userId) =>
        _refreshTokenRepository.RevokeAllForUserAsync(userId);

    private (string RawToken, RefreshToken Record) CreateRefreshToken(Guid userId)
    {
        // 64 bytes aleatórios em base64url: impossível de adivinhar
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var record = new RefreshToken(userId, Hash(rawToken), DateTimeOffset.UtcNow.AddDays(_refreshTokenDays));
        return (rawToken, record);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}

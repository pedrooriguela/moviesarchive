using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Auth;

// Token de renovação da sessão. O valor em si só existe no cookie do navegador:
// o banco guarda o hash SHA-256, então um vazamento da tabela não abre sessões.
public class RefreshToken : Entity
{
    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; set; }
    public string TokenHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    // Preenchido quando o token é trocado por outro (rotação) ou a sessão é encerrada
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt == null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}

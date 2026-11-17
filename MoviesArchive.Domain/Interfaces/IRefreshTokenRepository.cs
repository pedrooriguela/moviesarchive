using MoviesArchive.Domain.Models.Auth;

namespace MoviesArchive.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);

    // Encerra todas as sessões do usuário (troca de senha, reuso de token, conta anonimizada)
    Task RevokeAllForUserAsync(Guid userId);

    // Limpeza: apaga os tokens do usuário que já venceram ou foram revogados há mais de um dia
    Task DeleteStaleForUserAsync(Guid userId);
}

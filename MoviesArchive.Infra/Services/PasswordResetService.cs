using System.Security.Cryptography;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Infra.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasherService  _passwordHasherService;

    public PasswordResetService(
        IUserRepository userRepository,
        IEmailService emailService,
        IPasswordHasherService passwordHasherService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _passwordHasherService = passwordHasherService;
    }

    public async Task AskPasswordResetAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null) return;
        
        await _userRepository.UpdatePasswordResetTokenAsync(user.Id, GerarToken(), DateTime.UtcNow.AddMinutes(30));
        
        var link = $"http://localhost:5173/redefinir-senha?token={user.PasswordResetToken}";
        await _emailService.SendAsync(user.Email, "Redefinição de senha",
            $"Clique no link para criar uma nova senha (vale por 30 minutos): {link}");
    }

    public async Task<bool> ResetPasswordAsync(string newPassword, string token)
    {
        var user = await _userRepository.GetByPasswordResetTokenAsync(token);
        if (user is null || user.PasswordResetExpiresAt < DateTime.UtcNow)
            return false;
        
        user.Password = _passwordHasherService.Hash(user, newPassword);
        await _userRepository.UpdatePasswordResetTokenAsync(user.Id, null, null);
        await _userRepository.UpdateAsync(user.Id, user.Name, user.Password);
        return true;
    }
    
    private string GerarToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    .Replace("+", "-").Replace("/", "_").Replace("=", "");

}
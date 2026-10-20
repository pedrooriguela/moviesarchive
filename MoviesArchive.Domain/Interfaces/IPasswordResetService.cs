namespace MoviesArchive.Domain.Interfaces;

public interface IPasswordResetService
{
    Task AskPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string newPassword, string token);
}
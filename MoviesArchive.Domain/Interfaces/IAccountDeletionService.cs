using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Domain.Interfaces;

public interface IAccountDeletionService
{
    /// <summary>
    /// Exclui a conta depois de conferir a senha. Lança WrongPasswordException se ela não bater
    /// e UserNotFoundException se a conta não existir.
    /// </summary>
    Task DeleteAccountAsync(Guid userId, string password, ContentDestination destination);
}

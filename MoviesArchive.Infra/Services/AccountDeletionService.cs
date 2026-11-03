using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;

namespace MoviesArchive.Infra.Services;

public class AccountDeletionService : IAccountDeletionService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;

    public AccountDeletionService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasherService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
    }

    public async Task DeleteAccountAsync(Guid userId, string password, ContentDestination destination)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new UserNotFoundException();

        // Estar logado não basta: a exclusão não tem volta, então a senha é conferida de novo
        if (string.IsNullOrEmpty(user.Password) ||
            !_passwordHasherService.Compare(user, user.Password, password))
            throw new WrongPasswordException();

        if (destination == ContentDestination.Delete)
        {
            await _userRepository.Delete(userId);
            return;
        }

        // A conta anonimizada recebe uma senha aleatória que ninguém conhece,
        // então não dá mais para entrar nela
        var unusablePassword = _passwordHasherService.Hash(user, Guid.NewGuid().ToString("N"));
        await _userRepository.AnonymizeAsync(userId, unusablePassword);
    }
}

using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Auth;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Domain.Services;
public class UserLoginService : IUserLoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IAuthTokenService _authTokenService;

    public UserLoginService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasherService,
        IAuthTokenService authTokenService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _authTokenService = authTokenService;
    }

    public async Task CreateAccountAsync(CreateUserRequestDto createUserRequestDto)
    {
        var birthDate = createUserRequestDto.BirthDate
            ?? throw new InvalidBirthDateException();
        if (!BirthDates.IsValid(birthDate, BirthDates.Today()))
            throw new InvalidBirthDateException();

        if (ReservedUserNames.IsReserved(createUserRequestDto.Name))
            throw new UsernameAlreadyInUseException();

        if (await _userRepository.GetByUsernameAsync(createUserRequestDto.Name) != null)
            throw new UsernameAlreadyInUseException();

        if (await _userRepository.GetByEmailAsync(createUserRequestDto.Email) != null)
            throw new EmailAlreadyInUseException();

        var newUser = User.Builder.Create(createUserRequestDto.Name, createUserRequestDto.Email);
        newUser.BirthDate = birthDate;

        var hashedPassword = _passwordHasherService.Hash(newUser, createUserRequestDto.Password);

        newUser.SetPassword(hashedPassword);

        await _userRepository.AddAsync(newUser);
    }

    // JWT curto + refresh token (que o controller grava num cookie httpOnly)
    public async Task<AuthTokens> LogInAsync(LogInUserRequestDto logInUserRequestDto)
    {
        var user = await _userRepository.GetByUsernameAsync(logInUserRequestDto.Name);

        if (user == null)
            throw new WrongUsernameOrPasswordException();

        if(!_passwordHasherService.Compare(user, user.Password, logInUserRequestDto.Password))
            throw new WrongUsernameOrPasswordException();

        return await _authTokenService.IssueAsync(user);
    }

}

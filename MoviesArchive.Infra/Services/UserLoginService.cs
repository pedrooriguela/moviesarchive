using MoviesArchive.Domain.Exceptions;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Domain.Services;
public class UserLoginService : IUserLoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasherService _passwordHasherService;
    private readonly IJwtService _jwtService;

    public UserLoginService(
        IUserRepository userRepository,
        IPasswordHasherService passwordHasherService,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasherService = passwordHasherService;
        _jwtService = jwtService;
    }

    public async Task CreateAccountAsync(CreateUserRequestDto createUserRequestDto)
    {
        if (await _userRepository.GetByUsernameAsync(createUserRequestDto.Name) != null)
            throw new UsernameAlreadyInUseException();

        if (await _userRepository.GetByEmailAsync(createUserRequestDto.Email) != null)
            throw new EmailAlreadyInUseException();

        var newUser = User.Builder.Create(createUserRequestDto.Name, createUserRequestDto.Email);

        var hashedPassword = _passwordHasherService.Hash(newUser, createUserRequestDto.Password);

        newUser.SetPassword(hashedPassword);

        await _userRepository.AddAsync(newUser);
    }

    public async Task<string> LogInAsync(LogInUserRequestDto logInUserRequestDto)
    {
        var user = await _userRepository.GetByUsernameAsync(logInUserRequestDto.Name);

        if (user == null)
            throw new WrongUsernameOrPasswordException();

        if(!_passwordHasherService.Compare(user, user.Password, logInUserRequestDto.Password))
            throw new WrongUsernameOrPasswordException();

        return _jwtService.GenerateToken(user);
    }

}

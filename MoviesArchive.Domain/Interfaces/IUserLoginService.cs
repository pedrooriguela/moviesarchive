using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;

namespace MoviesArchive.Domain.Interfaces;

public interface IUserLoginService
{
    Task CreateAccountAsync(CreateUserRequestDto createUserRequestDto);
    Task<string> LogInAsync(LogInUserRequestDto logInUserRequestDto);
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MoviesArchive.Domain.Interfaces;

namespace MoviesArchive.Api.Auth;

// Política dos endpoints do painel: [Authorize(Policy = AdminPolicy.Name)].
// Confere Users.IsAdmin no banco a cada requisição (e não no token), então promover ou tirar
// o acesso de alguém vale na hora. Quem não é admin recebe 403.
public static class AdminPolicy
{
    public const string Name = "Admin";
}

public class AdminRequirement : IAuthorizationRequirement
{
}

public class AdminRequirementHandler : AuthorizationHandler<AdminRequirement>
{
    private readonly IUserRepository _userRepository;

    public AdminRequirementHandler(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId))
            return;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user?.IsAdmin == true)
            context.Succeed(requirement);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoviesArchive.Api.Controllers;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users.Dtos;
using Xunit;

public class ProfilePrivacyTests
{
    private readonly Guid viewer = Guid.NewGuid();

    private ProfilePrivacyController Controller(PrivacyFake data, bool loggedIn = true, bool active = true) => new(data, new UsersFake(active))
    {
        ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(loggedIn
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, viewer.ToString())], "test")
            : new ClaimsIdentity()) } }
    };

    [Fact]
    public async Task AnonymousCannotReadOrApproveRequests()
    {
        var c = Controller(new(), false);
        Assert.IsType<UnauthorizedResult>(await c.GetProfileAsync(Guid.NewGuid()));
        Assert.IsType<UnauthorizedResult>(await c.GetRequestsAsync());
        Assert.IsType<UnauthorizedResult>(await c.AcceptAsync(Guid.NewGuid()));
        Assert.IsType<UnauthorizedResult>(await c.DeclineAsync(Guid.NewGuid()));
        Assert.IsType<UnauthorizedResult>(await c.SetPrivacyAsync(new() { IsPrivate = true }));
    }

    [Fact]
    public async Task DeletedUserCannotUseOldIdentity()
    {
        var c = Controller(new() { Active = false }, active: false);
        Assert.IsType<UnauthorizedResult>(await c.GetRequestsAsync());
        Assert.IsType<UnauthorizedResult>(await c.AcceptAsync(Guid.NewGuid()));
        Assert.IsType<UnauthorizedResult>(await c.DeclineAsync(Guid.NewGuid()));
        Assert.IsType<UnauthorizedResult>(await c.SetPrivacyAsync(new() { IsPrivate = true }));
    }

    [Fact]
    public async Task OwnerAlwaysComesFromJwt()
    {
        var data = new PrivacyFake();
        var c = Controller(data);
        var requester = Guid.NewGuid();
        Assert.IsType<NoContentResult>(await c.AcceptAsync(requester));
        Assert.Equal((viewer, requester), data.LastAnswer);
        Assert.IsType<NoContentResult>(await c.DeclineAsync(requester));
        Assert.Equal((viewer, requester), data.LastAnswer);
        Assert.IsType<NoContentResult>(await c.SetPrivacyAsync(new() { IsPrivate = true }));
        Assert.Equal((viewer, true), data.LastPrivacy);
    }

    [Fact]
    public async Task UnknownRequestIsNotApproved() =>
        Assert.IsType<NotFoundResult>(await Controller(new() { RequestExists = false }).AcceptAsync(Guid.NewGuid()));

    [Fact]
    public async Task MissingPrivacyAndInvalidPagesAreRejected()
    {
        var c = Controller(new());
        Assert.IsType<BadRequestObjectResult>(await c.SetPrivacyAsync(new()));
        Assert.IsType<BadRequestObjectResult>(await c.GetProfileAsync(Guid.NewGuid(), 0));
        Assert.IsType<BadRequestObjectResult>(await c.GetProfileAsync(Guid.NewGuid(), reviewsPage: 100001));
        Assert.IsType<BadRequestObjectResult>(await c.GetRequestsAsync(-1));
        Assert.IsType<BadRequestObjectResult>(await c.GetRequestsAsync(limit: 51));
    }

    [Fact]
    public void SharedProfileHasNoEmailOrPasswordFields()
    {
        var names = typeof(SharedProfileDto).GetProperties().Select(p => p.Name.ToLowerInvariant());
        Assert.DoesNotContain(names, n => n.Contains("email") || n.Contains("password") || n.Contains("token"));
    }

    private class PrivacyFake : IProfilePrivacyRepository
    {
        public bool Active { get; init; } = true;
        public bool RequestExists { get; init; } = true;
        public (Guid, Guid)? LastAnswer { get; private set; }
        public (Guid, bool)? LastPrivacy { get; private set; }
        public Task<bool> SetPrivacyAsync(Guid id, bool value) { LastPrivacy = (id, value); return Task.FromResult(Active); }
        public Task<SharedProfileDto?> GetProfileAsync(Guid a, Guid b, int c, int d) => Task.FromResult<SharedProfileDto?>(null);
        public Task<IReadOnlyList<SocialUserDto>> GetRequestsAsync(Guid a, int b, int c) => Task.FromResult<IReadOnlyList<SocialUserDto>>([]);
        public Task<bool> AcceptRequestAsync(Guid a, Guid b) { LastAnswer = (a, b); return Task.FromResult(RequestExists); }
        public Task DeclineRequestAsync(Guid a, Guid b) { LastAnswer = (a, b); return Task.CompletedTask; }
    }

    private class UsersFake(bool active) : IUserFollowRepository
    {
        public Task<bool> IsActiveUserAsync(Guid id) => Task.FromResult(active);
        public Task<IReadOnlyList<SocialUserDto>> FindAsync(Guid a, string b, string c, int d, int e) => Task.FromResult<IReadOnlyList<SocialUserDto>>([]);
        public Task FollowAsync(Guid a, Guid b) => Task.CompletedTask;
        public Task UnfollowAsync(Guid a, Guid b) => Task.CompletedTask;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesArchive.Api.Controllers;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Users.Dtos;
using MoviesArchive.Infra.Database;
using Xunit;

public class UserFollowTests
{
    private readonly Guid viewer = Guid.NewGuid();
    private readonly Guid target = Guid.NewGuid();

    private UserFollowController Controller(FakeRepository repository, bool authenticated = true)
    {
        var identity = authenticated
            ? new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, viewer.ToString())], "test")
            : new ClaimsIdentity();
        return new(repository) { ControllerContext = new() { HttpContext = new DefaultHttpContext
            { User = new ClaimsPrincipal(identity) } } };
    }

    [Fact]
    public void EndpointsRequireAuthentication() =>
        Assert.True(Attribute.IsDefined(typeof(UserFollowController), typeof(AuthorizeAttribute)));

    [Fact]
    public async Task InvalidIdentityCannotReadOrWrite()
    {
        var repo = new FakeRepository();
        var c = Controller(repo, false);
        Assert.IsType<UnauthorizedResult>(await c.FindAsync(null));
        Assert.IsType<UnauthorizedResult>(await c.FollowAsync(target));
        Assert.IsType<UnauthorizedResult>(await c.UnfollowAsync(target));
        Assert.Empty(repo.Links);
    }

    [Fact]
    public async Task RemovedAccountCannotUseOldIdentity()
    {
        var repo = new FakeRepository { Inactive = viewer };
        var c = Controller(repo);
        Assert.IsType<UnauthorizedResult>(await c.FindAsync(null));
        Assert.IsType<UnauthorizedResult>(await c.FollowAsync(target));
        Assert.IsType<UnauthorizedResult>(await c.UnfollowAsync(target));
    }

    [Fact]
    public async Task SelfFollowAndMissingTargetAreRejected()
    {
        var repo = new FakeRepository { Inactive = target };
        var c = Controller(repo);
        Assert.IsType<BadRequestObjectResult>(await c.FollowAsync(viewer));
        Assert.IsType<NotFoundObjectResult>(await c.FollowAsync(target));
        Assert.Empty(repo.Links);
    }

    [Fact]
    public async Task FollowerAlwaysComesFromIdentityAndRelationIsDirectional()
    {
        var repo = new FakeRepository();
        var c = Controller(repo);
        Assert.IsType<NoContentResult>(await c.FollowAsync(target));
        Assert.Contains((viewer, target), repo.Links);
        Assert.DoesNotContain((target, viewer), repo.Links);
        Assert.IsType<NoContentResult>(await c.UnfollowAsync(target));
        Assert.Empty(repo.Links);
    }

    [Theory]
    [InlineData("unknown", 0, 20)]
    [InlineData("all", -1, 20)]
    [InlineData("all", 0, 0)]
    [InlineData("all", 0, 51)]
    public async Task RejectsInvalidFilters(string scope, int offset, int limit) =>
        Assert.IsType<BadRequestObjectResult>(await Controller(new()).FindAsync(null, scope, offset, limit));

    [Fact]
    public async Task SearchIsTrimmedAndBounded()
    {
        var repo = new FakeRepository();
        var c = Controller(repo);
        Assert.IsType<BadRequestObjectResult>(await c.FindAsync(new string('x', 101)));
        Assert.IsType<OkObjectResult>(await c.FindAsync("  Ana  ", "followers", 20, 20));
        Assert.Equal((viewer, "Ana", "followers", 20, 20), repo.LastQuery);
    }

    [Fact]
    public void PublicDtoContainsNoPrivateFields() =>
        Assert.Equal(new[] { "Id", "IsFollowing", "Name", "PhotoUrl" },
            typeof(SocialUserDto).GetProperties().Select(p => p.Name).OrderBy(n => n));

    [Fact]
    public void ModelMatchesExistingFollowerTable()
    {
        using var db = new MoviesArchiveDbContext(new DbContextOptionsBuilder<MoviesArchiveDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused").Options);
        var entity = db.Model.FindEntityType(typeof(UserFollower))!;
        Assert.Equal("UserFollowers", entity.GetTableName());
        Assert.Equal(new[] { "FollowerId", "FollowingId" }, entity.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior));
        Assert.Contains("UserFollowers", db.Database.GenerateCreateScript());
    }

    private class FakeRepository : IUserFollowRepository
    {
        public Guid? Inactive { get; init; }
        public HashSet<(Guid, Guid)> Links { get; } = [];
        public (Guid, string, string, int, int)? LastQuery { get; private set; }
        public Task<bool> IsActiveUserAsync(Guid id) => Task.FromResult(id != Inactive);
        public Task<IReadOnlyList<SocialUserDto>> FindAsync(Guid viewerId, string search, string scope, int offset, int limit)
        {
            LastQuery = (viewerId, search, scope, offset, limit);
            return Task.FromResult<IReadOnlyList<SocialUserDto>>([]);
        }
        public Task FollowAsync(Guid a, Guid b) { Links.Add((a, b)); return Task.CompletedTask; }
        public Task UnfollowAsync(Guid a, Guid b) { Links.Remove((a, b)); return Task.CompletedTask; }
    }
}

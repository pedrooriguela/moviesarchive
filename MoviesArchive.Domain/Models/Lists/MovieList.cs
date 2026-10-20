using MoviesArchive.Domain.Common;

namespace MoviesArchive.Domain.Models.Lists;

public class MovieList : Entity
{
    public MovieList(
        Guid userId,
        string title,
        string? description,
        bool isPublic)
    {
        UserId = userId;
        Title = title;
        Description = description;
        IsPublic = isPublic;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid UserId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ListMovie> Items { get; set; } = new();

    public bool IsPrivate => !IsPublic;

    public bool IsOwnedBy(Guid userId) =>
        UserId == userId;

    public class Builder
    {
        public static MovieList Create(Guid userId, string title, string? description, bool isPublic) =>
            new(userId, title, description, isPublic);
    }
}

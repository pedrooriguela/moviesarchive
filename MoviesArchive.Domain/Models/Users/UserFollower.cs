namespace MoviesArchive.Domain.Models.Users;

public class UserFollower
{
    public Guid FollowerId { get; set; }
    public Guid FollowingId { get; set; }
    public bool IsAccepted { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

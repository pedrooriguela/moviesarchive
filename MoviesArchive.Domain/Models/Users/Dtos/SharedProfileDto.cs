using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Users.Dtos;

public class PrivacyRequestDto
{
    [Required]
    public bool? IsPrivate { get; set; }
}

public class SharedProfileDto : SocialUserDto
{
    public bool CanViewContent { get; set; }
    public int ListsPage { get; set; }
    public int ReviewsPage { get; set; }
    public int ListsTotal { get; set; }
    public int ReviewsTotal { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public List<SharedListDto> Lists { get; set; } = [];
    public List<SharedReviewDto> Reviews { get; set; } = [];
}

public record SharedListDto(Guid Id, string Title, string? Description, int MovieCount);
public record SharedReviewDto(Guid Id, Guid MovieId, string MovieTitle, decimal Rating, string? Comment, DateTimeOffset CreatedAt);

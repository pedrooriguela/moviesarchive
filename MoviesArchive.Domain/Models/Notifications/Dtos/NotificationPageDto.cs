namespace MoviesArchive.Domain.Models.Notifications.Dtos;

public class NotificationPageDto
{
    public List<NotificationResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int UnreadCount { get; set; }
}

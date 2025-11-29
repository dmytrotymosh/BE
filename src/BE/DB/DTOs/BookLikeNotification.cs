namespace DB.DTOs;

public class BookLikeNotification
{
    public string Message { get; set; } = string.Empty;
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public Guid LikedByUserId { get; set; }
    public string LikedByUserFirstName { get; set; } = string.Empty;
    public string LikedByUserLastName { get; set; } = string.Empty;
}

using DB.Models.Enums;

namespace DB.DTOs;

public class SwipeResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public SwipeType SwipeType { get; set; }
    public DateTime Created { get; set; }
}

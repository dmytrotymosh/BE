namespace DB.DTOs;

public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Img { get; set; }
    public string? Description { get; set; }
    public DateTime Created { get; set; }
}

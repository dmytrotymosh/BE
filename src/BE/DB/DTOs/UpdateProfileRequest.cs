using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class UpdateProfileRequest
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(50)]
    public string? TimeZone { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    public string? Img { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class CreateSupportTicketRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MinLength(5, ErrorMessage = "Subject must be at least 5 characters long")]
    [MaxLength(200, ErrorMessage = "Subject must not exceed 200 characters")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [MinLength(10, ErrorMessage = "Message must be at least 10 characters long")]
    [MaxLength(5000, ErrorMessage = "Message must not exceed 5000 characters")]
    public string Message { get; set; } = string.Empty;
}

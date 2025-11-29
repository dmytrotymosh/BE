using DB.Models.Enums;

namespace DB.DTOs;

public class SupportTicketResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserFirstName { get; set; } = string.Empty;
    public string UserLastName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public string? AdminResponse { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

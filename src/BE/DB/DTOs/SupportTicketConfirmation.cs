namespace DB.DTOs;

public class SupportTicketConfirmation
{
    public Guid TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}

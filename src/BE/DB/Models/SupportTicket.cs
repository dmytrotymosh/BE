using System.ComponentModel.DataAnnotations.Schema;
using DB.Models.Enums;

namespace DB.Models;

public class SupportTicket : BaseEntity
{
    [Column("user_id", TypeName = "uuid")]
    public Guid UserId { get; set; }
    public User User { get; set; }

    [Column("subject", TypeName = "varchar(200)")]
    public string Subject { get; set; } = string.Empty;

    [Column("message", TypeName = "text")]
    public string Message { get; set; } = string.Empty;

    [Column("status", TypeName = "integer")]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Column("admin_response", TypeName = "text")]
    public string? AdminResponse { get; set; }

    [Column("responded_at", TypeName = "timestamp")]
    public DateTime? RespondedAt { get; set; }
}

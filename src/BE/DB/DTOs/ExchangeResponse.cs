namespace DB.DTOs;

using DB.Models.Enums;

public class ExchangeResponse
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle {  get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string OwnerFirstName { get; set; } = string.Empty;
    public string OwnerLastName { get; set; } = string.Empty;
    public Guid ReceiverId { get; set; }
    public string ReceiverFirstName { get; set; } = string.Empty;
    public string ReceiverLastName { get; set; } = string.Empty;
    public ExchangeStatus Status { get; set; }
    public int Rating { get; set; } = default(int);
    public string Comment { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}
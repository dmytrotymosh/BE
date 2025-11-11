using DB.Models.Enums;

namespace DB.DTOs;

public class ExchangeFilterRequest
{
    public Guid? UserId { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? ReceiverId { get; set; }
    public Guid? BookId { get; set; }
    public ExchangeStatus? status { get; set; }
    public DateTime? CreatedFrom {  get; set; }
    public DateTime? CreatedTo { get; set; }
}
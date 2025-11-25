using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class CreateChatRequest
{
    [Required]
    public Guid ExchangeId { get; set; }
}

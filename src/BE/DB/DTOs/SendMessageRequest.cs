using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class SendMessageRequest
{
    [Required]
    public Guid ChatId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;
}

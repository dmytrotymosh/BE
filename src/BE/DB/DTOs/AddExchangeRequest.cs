using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class AddExchangeRequest
{
    [Required]
    public Guid BookId { get; set; } = default;

    [Required]
    public Guid OwnerId { get; set; } = default;
}
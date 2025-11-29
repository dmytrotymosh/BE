using System.ComponentModel.DataAnnotations;
using DB.Models.Enums;

namespace DB.DTOs;

public class SwipeRequest
{
    [Required]
    public Guid BookId { get; set; }

    [Required]
    public SwipeType SwipeType { get; set; }
}

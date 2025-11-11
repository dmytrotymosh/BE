using System.ComponentModel.DataAnnotations;
using DB.Models.Enums;

namespace DB.DTOs;

public class UpdateExchangeRequest
{
    public ExchangeStatus? Status { get; set; }

    public int? Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }
}
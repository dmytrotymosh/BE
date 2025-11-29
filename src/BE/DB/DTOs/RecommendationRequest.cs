using System.ComponentModel.DataAnnotations;

namespace DB.DTOs;

public class RecommendationRequest
{
    [Required(ErrorMessage = "Query is required")]
    [MinLength(3, ErrorMessage = "Query must be at least 3 characters long")]
    [MaxLength(500, ErrorMessage = "Query must not exceed 500 characters")]
    public string Query { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Limit must be between 1 and 20")]
    public int Limit { get; set; } = 5;
}

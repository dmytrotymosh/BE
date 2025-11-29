namespace DB.DTOs;

public class RecommendationResponse
{
    public string Query { get; set; } = string.Empty;
    public int TotalResults { get; set; }
    public IEnumerable<BookRecommendation> Recommendations { get; set; } = new List<BookRecommendation>();
}

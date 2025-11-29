namespace DB.DTOs;

public class ExchangeDetailsResponse
{
    public ExchangeResponse Exchange { get; set; } = null!;
    public UserProfileResponse RequesterProfile { get; set; } = null!;
    public IEnumerable<BookResponse> RequesterBooks { get; set; } = new List<BookResponse>();
}

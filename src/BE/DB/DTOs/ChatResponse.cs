namespace DB.DTOs;

public class ChatResponse
{
    public Guid Id { get; set; }
    public Guid UserId1 { get; set; }
    public string User1FirstName { get; set; } = string.Empty;
    public string User1LastName { get; set; } = string.Empty;
    public Guid UserId2 { get; set; }
    public string User2FirstName { get; set; } = string.Empty;
    public string User2LastName { get; set; } = string.Empty;
    public Guid? ExchangeId { get; set; }
    public MessageResponse? LastMessage { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

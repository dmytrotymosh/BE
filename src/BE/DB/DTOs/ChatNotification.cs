namespace DB.DTOs;

public class ChatNotification
{
    public Guid ChatId { get; set; }
    public MessageResponse Message { get; set; } = new();
}

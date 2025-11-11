namespace DB.DTOs;

public class ExchangeNotification
{
    public string Message { get; set; }
    public ExchangeResponse Data {  get; set; }
}
namespace BlockMessages;

public class Message
{
    public Message(string data)
    {
        Data = data;
    }

    public string Data { get; set; }

    public string? Date { get; set; } = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
}

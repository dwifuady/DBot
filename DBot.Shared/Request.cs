namespace DBot.Shared;
public class Request
{
    public Request(string message)
    {
        Message = message;
        Messages = new List<string>
        {
            message
        };
    }

    public Request(IReadOnlyList<string> messages)
    {
        Message = messages.ToList().LastOrDefault()!;
        Messages = messages;
    }

    public string Message { get; }
    public IReadOnlyList<string> Messages { get; }
}

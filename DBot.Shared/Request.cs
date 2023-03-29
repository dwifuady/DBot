using System.Diagnostics.CodeAnalysis;

namespace DBot.Shared;
public sealed class Request : IRequest, IParsable<Request>
{
    private Request(string message, string command, string args)
    {
        Message = message;
        Command = command;
        Args = args;
        FullArgs = args;
    }

    private Request(string message, string command, string args, IEnumerable<RequestMessage> messages, string fullArgs) : this(message, command, args)
    {
        Messages = messages;
        FullArgs = fullArgs;
    }

    public string Command { get; private set;}
    public string Args { get; private set;}
    public string Message { get; }
    public string FullArgs { get; }
    public IEnumerable<RequestMessage>? Messages { get; private set; }

    public bool UpdateArgs(string newArgs)
    {
        try
        {
            Args = newArgs;
            Messages = new List<RequestMessage>
            {
                new RequestMessage(Sender.User, newArgs, 1, Command)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateMessageChain(IEnumerable<RequestMessage> messages)
    {
        try
        {
            if (!messages.Any())
            {
                return true;
            }
            Messages = messages;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UpdateCommand(string command)
    {
        Command = command;
        return true;
    }

    public static Request Parse(string s, IFormatProvider? provider)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            throw new Exception("Message can't be empty");
        }

        string[] strings = s.Split(new[] {',',' ', ':'});
        if (strings.Length < 1)
        {
            throw new OverflowException($"Invalid input parameter {s}");
        }

        string command = strings[0];
        var i = s.IndexOf(strings?.FirstOrDefault(x => x == " ") ?? " ", StringComparison.Ordinal) + 1;

        return new Request(s, command, s[i..],  new List<RequestMessage>
            {
                new RequestMessage(Sender.User, s[i..], 1, command)
            }, s);
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Request result)
    {
        result = null;
        if (s == null)
        {
            return false;
        }
        try
        {
            result = Parse(s, provider);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public record MessageChain(List<RequestMessage> RequestMessages);
    public record RequestMessage(Sender Sender, string Message, int Sequence, string InitialCommand);

    public enum Sender
    {
        Bot,
        User
    }
}

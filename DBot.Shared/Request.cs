using System.Diagnostics.CodeAnalysis;

namespace DBot.Shared;
public sealed class Request : IRequest, IParsable<Request>
{
    private Request(string message, string command, string args)
    {
        Message = message;
        Command = command;
        Args = args;
    }

    public string Command { get; }
    public string Args { get; private set;}
    public string Message { get; }

    public bool UpdateArgs(string newArgs)
    {
        try
        {
            Args = newArgs;
            return true;
        }
        catch
        {
            return false;
        }
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

        return new Request(s, command, s[i..]);
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
}

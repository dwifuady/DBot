using static DBot.Shared.Request;

namespace DBot.Shared;

public interface IRequest
{
    public string Message { get; }
    public string Command { get; }
    public string Args { get; }
    IEnumerable<RequestMessage>? Messages{ get; }
}

using System.Threading.Tasks;

namespace DBot.Shared;

public interface ICommand
{
    string Command { get; }
    Task<IResponse> ExecuteCommand(Request request);
}

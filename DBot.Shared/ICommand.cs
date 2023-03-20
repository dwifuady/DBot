namespace DBot.Shared;

public interface ICommand
{
    IReadOnlyList<string> AcceptedCommands { get; }
    Task<IResponse> ExecuteCommand(IRequest request);
}

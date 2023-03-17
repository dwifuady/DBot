namespace DBot.Shared;

public interface ICommand
{
    string Command { get; }
    Task<IResponse> ExecuteCommand(Request request);
}

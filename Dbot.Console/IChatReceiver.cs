namespace DBot.Console;

public interface IChatReceiver
{
    Task StartReceiving(CancellationToken cancellationToken);
}

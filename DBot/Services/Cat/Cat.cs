using DBot.Shared;

namespace DBot.Services.Cat;

public class Cat : ICommand
{
    public string Command => "cat:|kucing:";

    public async Task<IResponse> ExecuteCommand(Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return await Task.FromResult(new ImageResponse("https://cataas.com/cat", ""));
        }
        else
        {
            return await Task.FromResult(new ImageResponse($"https://cataas.com/cat/says/{request.Message}", ""));
        }
    }
}

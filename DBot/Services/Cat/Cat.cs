using DBot.Shared;

namespace DBot.Services.Cat;

public class Cat : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "CAT:", "KUCING:", "CAT", "KUCING" };

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Args))
        {
            return await Task.FromResult(new ImageResponse(true, "https://cataas.com/cat", ""));
        }

        return await Task.FromResult(new ImageResponse(true, $"https://cataas.com/cat/says/{request.Args}", ""));
    }
}
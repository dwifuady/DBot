using DBot.Shared;

namespace DBot.Services.HelloWorld;

public class HelloWorld : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "/hello" };

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        var response =  new TextResponse(true, $"Hello world. You said {request.Message}");
        return await Task.FromResult(response);
    }
}
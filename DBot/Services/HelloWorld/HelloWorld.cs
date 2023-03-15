using DBot.Shared;

namespace DBot.Services.HelloWorld;

public class HelloWorld : ICommand
{
    public string Command => "/hello";

    public async Task<IResponse> ExecuteCommand(Request request)
    {
        var response =  new TextResponse
        {
            Message = $"Hello world. You said {request.Message}"
        };
        return await Task.FromResult(response);
    }
}
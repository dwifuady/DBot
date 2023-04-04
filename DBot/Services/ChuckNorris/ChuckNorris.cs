using DBot.Shared;

namespace DBot.Services.ChuckNorris;

public class ChuckNorris : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "CHUCK", "CHUCKNORRIS" };
    private readonly IChuckNorrisApi _chuckNorrisApi;

    public ChuckNorris(IChuckNorrisApi chuckNorrisApi)
    {
        _chuckNorrisApi = chuckNorrisApi;
    }

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        var response = await _chuckNorrisApi.GetRandom();
        if (response is not null && !string.IsNullOrWhiteSpace(response.Value))
        {
            return new TextResponse(true, response.Value);
        }
        else
        {
            return new TextResponse(false, "no jokes for now");
        }
    }
}

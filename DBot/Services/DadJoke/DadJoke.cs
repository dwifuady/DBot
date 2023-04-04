using DBot.Shared;

namespace DBot.Services.DadJoke;

public class DadJoke : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "DADJOKE" };
    private readonly IDadJokeApi _dadJokeApi;

    public DadJoke(IDadJokeApi dadJokeApi)
    {
        _dadJokeApi = dadJokeApi;
    }

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        var headers = new Dictionary<string, string> {{"Accept","application/json"}, {"User-Agent","https://github.com/dwifuady/DBot"}};
        var response = await _dadJokeApi.GetRandom(headers);

        if (response is not null && !string.IsNullOrWhiteSpace(response.Joke))
        {
            return new TextResponse(true, response.Joke);
        }
        else
        {
            return new TextResponse(false, "no jokes for now");
        }
    }
}

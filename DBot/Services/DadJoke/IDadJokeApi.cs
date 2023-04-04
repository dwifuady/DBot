using Refit;

namespace DBot.Services.DadJoke;

public interface IDadJokeApi
{
    [Get("/")]
    Task<DadJokeApiResponse> GetRandom([HeaderCollection] IDictionary<string, string> headers);
}

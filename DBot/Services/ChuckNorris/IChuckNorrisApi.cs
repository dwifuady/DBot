using Refit;

namespace DBot.Services.ChuckNorris;

public interface IChuckNorrisApi
{
    [Get("/jokes/random")]
    Task<ChuckNorrisApiResponse> GetRandom();
}

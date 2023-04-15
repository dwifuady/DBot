using Refit;

namespace DBot.Services.Dog;

public interface IDogApi
{
    [Get("/woof.json")]
    Task<DogApiResponse> Get();
}

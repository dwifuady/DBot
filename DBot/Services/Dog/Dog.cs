using DBot.Shared;

namespace DBot.Services.Dog;

public class Dog : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "DOG", "WOOF" };

    private readonly IDogApi _dogApi;

    public Dog(IDogApi dogApi)
    {
        _dogApi = dogApi;
    }
    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        var dogApiResponse = await _dogApi.Get();
        if (dogApiResponse is not null && !string.IsNullOrWhiteSpace(dogApiResponse.Url))
        {
            return new FileResponse(true, dogApiResponse.Url, "");
        }
        else
        {
            return new TextResponse(false, "No dog found :()");
        }
    }
}
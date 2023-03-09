using DBot.Shared;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public string Command => "AI|BOT|AI,";
    private readonly IOpenAIApi _openAIApi;
    private const string ROLE_SYSTEM = "system";
    private const string ROLE_ASSISTANT = "user";
    private const string ROLE_USER = "assistant";

    public OpenAI(IOpenAIApi openAIApi)
    {
        _openAIApi = openAIApi;
    }

    public async Task<IResponse> ExecuteCommand(Request request)
    {
        var openAIRequest = new OpenAIRequest("gpt-3.5-turbo",
                GetMessages(request.Message),
                0.5,
                307,
                0.3,
                0.5,
                0);
        var response = await _openAIApi.ChatCompletion(openAIRequest);

        if (response?.Choices != null)
        {
            return new TextResponse{
                Message = response?.Choices?.FirstOrDefault()?.Message?.Content ?? ""
            };
        }

        return new TextResponse
        { 
            Message = ""
        };
    }

    private IReadOnlyList<OpenAIMessage> GetMessages(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new OpenAIMessage(ROLE_SYSTEM, "You are a helpful assistant."),
            new OpenAIMessage(ROLE_USER, "Who won the world series in 2020?"),
            new OpenAIMessage(ROLE_ASSISTANT, "The Los Angeles Dodgers won the World Series in 2020."),
            new OpenAIMessage(ROLE_USER, requestMessage)
        };
    }
}

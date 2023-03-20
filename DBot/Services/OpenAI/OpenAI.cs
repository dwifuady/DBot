using System.Text.Json;
using DBot.Shared;
using Serilog;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "AI", "BOT", "AI,", "ENID", "IDEN", "WDYT", "KOMENTARIN" };
    private readonly IOpenAIApi _openAIApi;
    private const string RoleSystem = "system";
    private const string RoleAssistant = "assistant";
    private const string RoleUser = "user";

    public OpenAI(IOpenAIApi openAIApi)
    {
        _openAIApi = openAIApi;
    }

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        try
        {
            var response = await GetChatCompletion(request.Args);

            if (response?.Choices != null)
            {
                return new TextResponse{
                    Message = response?.Choices?.FirstOrDefault()?.Message?.Content ?? ""
                };
            }

            return new TextResponse
            {
                Message = "I am confuse. Could you try to ask another question?"
            };
        }
        catch (Exception e)
        {
            Log.Error(e, "Error getting response from OpenAI");
            return new TextResponse
            {
                Message = """
                I am sorry, I can't think right now :(
                Please try again later.
                """
            };
        }
    }

    private async Task<OpenAIResponse> GetChatCompletion(string message)
    {
        var openAIRequest = new OpenAIRequest("gpt-3.5-turbo",
                GetChatCompletionMessages(message),
                0.5,
                500,
                0.3,
                0.5,
                0);

        Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAIRequest));

        var response = await _openAIApi.ChatCompletion(openAIRequest);

        Log.Information("OpenAI Response {response}", JsonSerializer.Serialize(response));

        return response;
    }

    private static IReadOnlyList<OpenAIMessage> GetChatCompletionMessages(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a helpful assistant."),
            new(RoleUser, requestMessage)
        };
    }
}

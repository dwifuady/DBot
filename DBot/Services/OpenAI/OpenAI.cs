using System.Text.Json;
using DBot.Shared;
using Serilog;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public string Command => "AI|BOT|AI,";
    private readonly IOpenAIApi _openAIApi;
    private const string RoleSystem = "system";
    private const string RoleAssistant = "assistant";
    private const string RoleUser = "user";

    public OpenAI(IOpenAIApi openAIApi)
    {
        _openAIApi = openAIApi;
    }

    public async Task<IResponse> ExecuteCommand(Request request)
    {
        try
        {
            var openAIRequest = new OpenAIRequest("gpt-3.5-turbo",
                    GetMessages(request.Message),
                    0.5,
                    307,
                    0.3,
                    0.5,
                    0);

            Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAIRequest));

            var response = await _openAIApi.ChatCompletion(openAIRequest);

            Log.Information("OpenAI Response {response}", JsonSerializer.Serialize(response));

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
                Message = @"I am sorry, I can't think right now :(
Please try again later."
            };
        }
    }

    private IReadOnlyList<OpenAIMessage> GetMessages(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a helpful assistant."),
            new(RoleUser, "Who won the world series in 2020?"),
            new(RoleAssistant, "The Los Angeles Dodgers won the World Series in 2020."),
            new(RoleUser, requestMessage)
        };
    }
}

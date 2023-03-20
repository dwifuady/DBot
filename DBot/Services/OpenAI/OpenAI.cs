using System.Text.Json;
using DBot.Shared;
using Serilog;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "AI", "KAKBOT", "PAKBOT", "ENID", "IDEN", "WDYT", "KOMENTARIN", "KOMENIN", "KOMENNYA" };
    private readonly IOpenAIApi _openAIApi;
    private const string RoleSystem = "system";
    //private const string RoleAssistant = "assistant";
    private const string RoleUser = "user";

    public OpenAI(IOpenAIApi openAIApi)
    {
        _openAIApi = openAIApi;
    }

    public async Task<IResponse> ExecuteCommand(IRequest request)
    {
        try
        {
            var response = await GetChatCompletion(request.Command, request.Args);

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

    private async Task<OpenAIResponse> GetChatCompletion(string command, string message)
    {
        var openAIRequest = new OpenAIRequest("gpt-3.5-turbo",
                GetChatCompletionMessages(command, message),
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

    private static IReadOnlyList<OpenAIMessage> GetChatCompletionMessages(string command, string requestMessage)
    {
        return command.ToUpper() switch
        {
            "AI" => GetDefaultMessage(requestMessage),
            "ENID" => GetEnIdMessage(requestMessage),
            "IDEN" => GetIdEnMessage(requestMessage),
            "WDYT" => GetWdytMessage(requestMessage),
            "KOMENTARIN" or "KOMENIN" or "KOMENNYA" => GetWdytIdMessage(requestMessage),
            "KAKBOT" => GetFriendlyAssistantMessage(requestMessage),
            "PAKBOT" => GetGrumpyAssistantMessage(requestMessage),
            _ => GetDefaultMessage(requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetDefaultMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny and helpful assistant."),
            new(RoleUser, requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetFriendlyAssistantMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a helpful and funny assistant who speaks Indonesia casually, not using formal language"),
            new(RoleUser, requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetGrumpyAssistantMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a grumpy assistant who speaks Indonesia and answer the question with direct and short answer"),
            new(RoleUser, requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetEnIdMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who translate from English to Indonesian language."),
            new(RoleUser, requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetIdEnMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who translate from Indonesia to English language."),
            new(RoleUser, requestMessage)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetWdytMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny Assistant who always give funny comment about everything"),
            new(RoleUser, $"What do you think about this: '{requestMessage}'")
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetWdytIdMessage(string requestMessage)
    {
        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny Assistant who always give funny comment about everything"),
            new(RoleUser, $"Apa komentar kamu tentang ini: '{requestMessage}'")
        };
    }
}

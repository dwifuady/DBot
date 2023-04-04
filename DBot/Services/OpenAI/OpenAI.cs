using System.Text.Json;
using DBot.Shared;
using Refit;
using Serilog;
using static DBot.Shared.Request;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string> { "AI", "KAKBOT", "PAKBOT", "ENID", "IDEN", "WDYT", "KOMENTARIN", "KOMENIN", "KOMENNYA" };
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
        if (request is null || request.Messages is null || request?.Messages?.Any() == false)
        {
            return new TextResponse(false, "Sorry, something went wrong. I can't see your message");
        }

        var (IsSuccess, Response, OpenAiErrorResponse) = await GetChatCompletion(request!.Command, request.Messages);

        if (IsSuccess && Response?.Choices != null)
        {
            return new TextResponse(true, Response.Choices?.FirstOrDefault()?.Message?.Content ?? "", true);
        }
        else if (!IsSuccess)
        {
            return new TextResponse(false, $"Sorry, there are issues when trying to get response from OpenAI api. Error: {OpenAiErrorResponse?.Error?.ErrorType}");
        }

        return new TextResponse(false, "I am confuse. Could you try to ask another question?");
    }

    private async Task<(bool IsSuccess, OpenAIResponse? Response, OpenAIError? error)> GetChatCompletion(string command, IEnumerable<RequestMessage> requestMessages)
    {
        var openAIRequest = new OpenAIRequest("gpt-3.5-turbo",
                GetChatCompletionMessages(command, requestMessages),
                0.5,
                500,
                0.3,
                0.5,
                0);

        Log.Information("OpenAI Request {request}", JsonSerializer.Serialize(openAIRequest));

        try
        {
            var response = await _openAIApi.ChatCompletion(openAIRequest);
            Log.Information("OpenAI Response {response}", JsonSerializer.Serialize(response));

            return new (true, response, null);
        }
        catch (ApiException exception)
        {
            if (!string.IsNullOrWhiteSpace(exception?.Content))
            {
                var error = JsonSerializer.Deserialize<OpenAIError>(exception.Content);
                Log.Error(exception, exception.Content);
                return new (false, null, error);
            }
            else
            {
                Log.Error(exception, exception?.Message ?? string.Empty);
                return new (false, null, new OpenAIError { Error = new Error { Message = exception?.Message} });
            }
        }
    }

    private static IReadOnlyList<OpenAIMessage> GetChatCompletionMessages(string command, IEnumerable<RequestMessage> requestMessages)
    {
        return command.ToUpper() switch
        {
            "AI" => GetDefaultMessage(requestMessages),
            "ENID" => GetEnIdMessage(requestMessages),
            "IDEN" => GetIdEnMessage(requestMessages),
            "WDYT" => GetWdytMessage(requestMessages),
            "KOMENTARIN" or "KOMENIN" or "KOMENNYA" => GetWdytIdMessage(requestMessages),
            "KAKBOT" => GetFriendlyAssistantMessage(requestMessages),
            "PAKBOT" => GetGrumpyAssistantMessage(requestMessages),
            _ => GetDefaultMessage(requestMessages)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetDefaultMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny and helpful assistant.")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            message.Add(new(GetRole(requestMessage.Sender), requestMessage.Message));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetFriendlyAssistantMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a helpful and funny assistant who speaks Indonesia casually, not using formal language")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            message.Add(new(GetRole(requestMessage.Sender), requestMessage.Message));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetGrumpyAssistantMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a grumpy assistant who speaks Indonesia and answer the question with direct and short answer")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            message.Add(new(GetRole(requestMessage.Sender), requestMessage.Message));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetEnIdMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var requestMessage = requestMessages.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Sender == Sender.User)?.Message;

        if (string.IsNullOrWhiteSpace(requestMessage))
        {
            throw new Exception("No message to translate");
        }

        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who translate from English to Indonesian language."),
            new(RoleUser, "translate this: Good Morning"),
            new(RoleAssistant, "Selamat Pagi"),
            new(RoleUser, $"translate this: {requestMessage}")
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetIdEnMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var requestMessage = requestMessages.OrderBy(x => x.Sequence).FirstOrDefault(x => x.Sender == Sender.User)?.Message;

        if (string.IsNullOrWhiteSpace(requestMessage))
        {
            throw new Exception("No message to translate");
        }

        return new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who translate from Indonesia to English language."),
            new(RoleUser, "translate this: Selamat Pagi"),
            new(RoleAssistant, "Good Morning"),
            new(RoleUser, $"translate this: {requestMessage}")
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetWdytMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny Assistant who always give funny comment about everything"),
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            var msg = requestMessage.Message;
            if (requestMessage.Sequence == 1 && requestMessage.Sender == Sender.User)
            {
                msg = $"What do you think about this: '{requestMessage.Message}'";
            }
            message.Add(new(GetRole(requestMessage.Sender), msg));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetWdytIdMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a funny Assistant who always give funny comment about everything")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            var msg = requestMessage.Message;
            if (requestMessage.Sequence == 1 && requestMessage.Sender == Sender.User)
            {
                msg = $"Apa komentar kamu tentang ini: '{requestMessage.Message}'";
            }
            message.Add(new(GetRole(requestMessage.Sender), msg));
        }

        return message;
    }

    private static string GetRole(Sender sender)
    {
        return sender switch
        {
            Sender.Bot => RoleAssistant,
            Sender.User => RoleUser,
            _ => RoleUser
        };
    }
}

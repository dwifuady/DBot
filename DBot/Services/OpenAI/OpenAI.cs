using System.Text.Json;
using DBot.Shared;
using Refit;
using Serilog;
using static DBot.Shared.Request;

namespace DBot.Services.OpenAI;

public class OpenAI : ICommand
{
    public IReadOnlyList<string> AcceptedCommands => new List<string>
    {
        "AI",
        "KAKBOT",
        "PAKBOT",
        "ENID",
        "IDEN",
        "WDYT",
        "KOMENTARIN",
        "KOMENIN",
        "KOMENNYA",
        "CODEREVIEW",
        "CODEEXPLAIN",
        "MESSAGEASSIST",
        "CHATASSIST",
        "JELASIN",
        "ELI5ID"
    };

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
                1000,
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
            "CODEREVIEW" => GetCodeReviewMessage(requestMessages),
            "CODEEXPLAIN" => GetCodeExplainMessage(requestMessages),
            "MESSAGEASSIST" or "CHATASSIST" => GetChatReviewMessage(requestMessages),
            "ELI5ID" or "JELASIN" => GetELI5IdMessage(requestMessages),
            _ => GetDefaultMessage(requestMessages)
        };
    }

    private static IReadOnlyList<OpenAIMessage> GetDefaultMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a helpful assistant.")
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
            new(RoleSystem, "You are a sarcastic and grumpy assistant who speaks Indonesia and answer the question with direct and short answer")
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
            new(RoleSystem, "You are a funny Assistant who always give comment about everything"),
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
            new(RoleSystem, "You are a funny Assistant who always give comment about everything")
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

    private static IReadOnlyList<OpenAIMessage> GetCodeReviewMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who can review given code, give feedback based on best practice such as variable naming or others, do some refactoring, find possible bug and suggest solution to make the code better."),
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            var msg = requestMessage.Message;
            if (requestMessage.Sequence == 1 && requestMessage.Sender == Sender.User)
            {
                msg = $"Please review this code: '{requestMessage.Message}'";
            }
            message.Add(new(GetRole(requestMessage.Sender), msg));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetCodeExplainMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are an Assistant who can explain given code or sql query clearly"),
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            var msg = requestMessage.Message;
            if (requestMessage.Sequence == 1 && requestMessage.Sender == Sender.User)
            {
                msg = $"Please explain this code: '{requestMessage.Message}'";
            }
            message.Add(new(GetRole(requestMessage.Sender), msg));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetChatReviewMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleSystem, "You are a personal assistant who help user writing and reviewing a message, email, or a chat."),
            new(RoleUser, "You are my personal assistant who help me writing and reviewing a message, email, or a chat from me to other people. I am not really good at English, so I need your help to correct the grammar and make my sentences clear for the reader. are you ready?"),
            new(RoleAssistant, "Yes, I'm ready to assist you! Please let me know what you need help with.")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            message.Add(new(GetRole(requestMessage.Sender), requestMessage.Message));
        }

        return message;
    }

    private static IReadOnlyList<OpenAIMessage> GetELI5IdMessage(IEnumerable<RequestMessage> requestMessages)
    {
        var message = new List<OpenAIMessage>
        {
            new(RoleUser, "Aku akan memberikan topik atau pertanyaan. Jelaskan seolah olah aku adalah anak 5 tahun. Jelaskan dengan bahasa yang mudah dan sederhana, dengan contoh atau analogi yang sederhana."),
            new(RoleAssistant, "Tentu, saya akan berusaha untuk menjelaskan dengan bahasa yang mudah dipahami dan memberikan contoh yang sederhana. Silakan berikan topik atau pertanyaannya.")
        };

        foreach (var requestMessage in requestMessages.OrderBy(x => x.Sequence))
        {
            message.Add(new(GetRole(requestMessage.Sender), requestMessage.Message));
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

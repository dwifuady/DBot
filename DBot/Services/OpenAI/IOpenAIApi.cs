using Refit;

namespace DBot.Services.OpenAI;

public interface IOpenAIApi
{
    [Post("/v1/chat/completions")]
    [Headers("Authorization: Bearer")]
    Task<OpenAIResponse> ChatCompletion(OpenAIRequest request);
}

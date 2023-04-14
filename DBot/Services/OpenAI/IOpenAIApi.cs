using Refit;

namespace DBot.Services.OpenAI;

public interface IOpenAIApi
{
    [Post("/v1/chat/completions")]
    [Headers("Authorization: Bearer")]
    Task<OpenAIResponse> ChatCompletion(OpenAIRequest request);

    [Post("/v1/images/generations")]
    [Headers("Authorization: Bearer")]
    Task<ImageGenerationResponse> GenerateImages(ImageGenerationRequest request);
}

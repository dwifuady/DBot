using System.Text.Json.Serialization;

namespace DBot.Services.OpenAI;

public class ImageGenerationRequest
{
    public ImageGenerationRequest(string prompt, int n, string size)
    {
        Prompt = prompt;
        N = n;
        Size = size;
    }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; }
    [JsonPropertyName("n")]
    public int N { get; }
    [JsonPropertyName("size")]
    public string? Size { get; }
}

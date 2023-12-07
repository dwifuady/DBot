using System.Text.Json.Serialization;

namespace DBot.Services.OpenAI;

public class ImageGenerationRequest
{
    public ImageGenerationRequest(string prompt, int n, string size)
    {
        Model = "dall-e-2";
        Prompt = prompt;
        N = n;
        Size = size;
    }

    public ImageGenerationRequest(string model, string prompt, int n, string size)
    {
        Model = model;
        Prompt = prompt;
        N = n;
        Size = size;
    }

    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("prompt")]
    public string? Prompt { get; }
    [JsonPropertyName("n")]
    public int N { get; }
    [JsonPropertyName("size")]
    public string? Size { get; }
}

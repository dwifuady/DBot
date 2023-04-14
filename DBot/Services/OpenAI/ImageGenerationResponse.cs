using System.Text.Json.Serialization;

namespace DBot.Services.OpenAI;

public class ImageGenerationResponse
{
    [JsonPropertyName("created")]
    public int Created { get; set; }

    [JsonPropertyName("data")]
    public List<ImagesData>? Data { get; set; }
}

public class ImagesData
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
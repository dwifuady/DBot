using System.Text.Json.Serialization;

namespace DBot.Services.Dog;

public class DogApiResponse
{
    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

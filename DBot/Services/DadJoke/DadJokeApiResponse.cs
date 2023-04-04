using System.Text.Json.Serialization;

namespace DBot.Services.DadJoke;

public class DadJokeApiResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("joke")]
    public string? Joke { get; set; }
    [JsonPropertyName("status")]
    public int Status { get; set; }
}
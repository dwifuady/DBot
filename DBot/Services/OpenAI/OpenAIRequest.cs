using System.Text.Json.Serialization;

namespace DBot.Services.OpenAI;
    public class OpenAIMessage
    {
        public OpenAIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        [JsonPropertyName("role")]
        public string Role { get; }

        [JsonPropertyName("content")]
        public string Content { get; }
    }

    public class OpenAIRequest
    {
        public OpenAIRequest(string model, IReadOnlyList<OpenAIMessage> messages, double temperature, int maxTokens, double topP, double frequencyPenalty, int presencePenalty)
        {
            Model = model;
            Messages = messages;
            Temperature = temperature;
            MaxTokens = maxTokens;
            TopP = topP;
            FrequencyPenalty = frequencyPenalty;
            PresencePenalty = presencePenalty;
        }

        [JsonPropertyName("model")]
        public string Model { get; }

        [JsonPropertyName("messages")]
        public IReadOnlyList<OpenAIMessage> Messages { get; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; }

        [JsonPropertyName("top_p")]
        public double TopP { get; }

        [JsonPropertyName("frequency_penalty")]
        public double FrequencyPenalty { get; }

        [JsonPropertyName("presence_penalty")]
        public int PresencePenalty { get; }
    }


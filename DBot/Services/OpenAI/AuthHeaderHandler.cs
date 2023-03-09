using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using DBot.Shared.Configs;

namespace DBot.Services.OpenAI;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly OpenAIConfig _config;
    public AuthHeaderHandler(IOptions<OpenAIConfig> openAiOptions)
    {
        _config = openAiOptions.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.Token);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

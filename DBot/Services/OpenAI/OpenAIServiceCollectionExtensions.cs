using Microsoft.Extensions.DependencyInjection;
using DBot.Shared;
using Refit;

namespace DBot.Services.OpenAI;

public static class OpenAIServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAI(this IServiceCollection services)
    {
        services.AddTransient<ICommand, OpenAI>();
        services.AddTransient<AuthHeaderHandler>();
        services.AddRefitClient<IOpenAIApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openai.com"))
            .AddHttpMessageHandler<AuthHeaderHandler>();
        return services;
    }
}

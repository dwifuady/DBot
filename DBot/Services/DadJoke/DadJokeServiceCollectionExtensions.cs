using DBot.Shared;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace DBot.Services.DadJoke;

public static class DadJokeServiceCollectionExtensions
{
    public static IServiceCollection AddDadJoke(this IServiceCollection services)
    {
        services.AddTransient<ICommand, DadJoke>();
        services.AddRefitClient<IDadJokeApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://icanhazdadjoke.com"));

        return services;
    }
}

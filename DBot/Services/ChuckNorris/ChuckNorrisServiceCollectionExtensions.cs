using DBot.Shared;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace DBot.Services.ChuckNorris;

public static class ChuckNorrisServiceCollectionExtensions
{
    public static IServiceCollection AddChuckNorris(this IServiceCollection services)
    {
        services.AddTransient<ICommand, ChuckNorris>();
        services.AddRefitClient<IChuckNorrisApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.chucknorris.io"));

        return services;
    }
}

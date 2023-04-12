using DBot.Shared;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace DBot.Services.Dog;

public static class DogServiceCollectionExtensions
{
    public static IServiceCollection AddDog(this IServiceCollection services)
    {
        services.AddTransient<ICommand, Dog>();
        services.AddRefitClient<IDogApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://random.dog"));

        return services;
    }
}

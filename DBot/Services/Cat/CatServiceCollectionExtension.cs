using Microsoft.Extensions.DependencyInjection;
using DBot.Shared;

namespace DBot.Services.Cat;

public static class CatServiceCollectionExtension
{
    public static IServiceCollection AddCat(this IServiceCollection services)
    {
        services.AddTransient<ICommand, Cat>();
        return services;
    }
}

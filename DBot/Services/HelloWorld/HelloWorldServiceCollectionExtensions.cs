using Microsoft.Extensions.DependencyInjection;
using DBot.Shared;

namespace DBot.Services.HelloWorld;

public static class HelloWorldServiceCollectionExtensions
{
    public static IServiceCollection AddHelloWorld(this IServiceCollection services)
    {
        services.AddTransient<ICommand, HelloWorld>();
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using DBot.Shared;
using Refit;

namespace DBot.Services.SiCepat;

public static class SiCepatServiceCollectionExtensions
{
    public static IServiceCollection AddSiCepat(this IServiceCollection services)
    {
        services.AddTransient<ICommand, SiCepat>();
        services.AddRefitClient<ISiCepatApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://content-main-api-production.sicepat.com"));
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace SailNora.Platform;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSailNoraPlatform(this IServiceCollection services)
    {
        services.AddSailNoraPlatformServices();
        return services;
    }
}

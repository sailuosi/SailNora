using Microsoft.Extensions.DependencyInjection;

namespace SailNora.Platform;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSailNoraPlatform(this IServiceCollection services)
    {
        // AuthService / DbInitializer 等由 Scrutor 按 IScoped 扫描注册
        return services;
    }
}

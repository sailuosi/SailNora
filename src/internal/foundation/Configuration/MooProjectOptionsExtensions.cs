using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SailNora.Foundation.Data;
using SailNora.Foundation.Options;

namespace SailNora.Foundation.Configuration;

public static class MooProjectOptionsExtensions
{
    public static IServiceCollection AddSailNoraProjectOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DBCoonfigOptions>(configuration.GetSection(DBCoonfigOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<HangfireHostOptions>(configuration.GetSection(HangfireHostOptions.SectionName));
        return services;
    }
}

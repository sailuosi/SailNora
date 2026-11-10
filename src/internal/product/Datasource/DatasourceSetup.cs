using Microsoft.Extensions.DependencyInjection;
using SailNora.Product.Datasource.Jobs;

namespace SailNora.Product.Datasource;

public static class DatasourceSetup
{
    public static IServiceCollection AddSailNoraDatasource(this IServiceCollection services)
    {
        services.AddHttpClient("datasource", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("sail-nora-datasource/0.1");
        });
        // Hangfire.Enabled=true 时由 App 覆盖为 HangfireDatasourceSyncEnqueue
        services.AddScoped<IDatasourceSyncEnqueue, InlineDatasourceSyncEnqueue>();
        return services;
    }
}

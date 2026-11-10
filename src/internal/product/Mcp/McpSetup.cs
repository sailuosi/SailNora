using Microsoft.Extensions.DependencyInjection;

namespace SailNora.Product.Mcp;

public static class McpSetup
{
    public static IServiceCollection AddSailNoraMcp(this IServiceCollection services)
    {
        services.AddHttpClient("mcp-outbound", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("sail-nora-mcp/0.1");
        });
        return services;
    }
}

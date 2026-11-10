using Microsoft.Extensions.DependencyInjection;
using SailNora.Product.Wiki.Jobs;

namespace SailNora.Product.Wiki;

public static class WikiSetup
{
    public static IServiceCollection AddSailNoraWiki(this IServiceCollection services)
    {
        // Hangfire.Enabled=true 时由 App.HangfireSetup 覆盖为 HangfireWikiIngestEnqueue
        services.AddScoped<IWikiIngestEnqueue, InlineWikiIngestEnqueue>();
        return services;
    }
}

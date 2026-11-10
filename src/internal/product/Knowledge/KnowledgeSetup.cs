using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SailNora.Foundation.Options;
using SailNora.Product.Knowledge.DocReader;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.Product.Knowledge;

public static class KnowledgeSetup
{
    public static IServiceCollection AddSailNoraKnowledge(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("docreader", (sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<DocReaderOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(opt.Address))
                client.BaseAddress = new Uri(opt.Address);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // DocumentProcessJob / KnowledgeService / LocalFileStorage / HttpDocReaderClient 由 Scrutor 扫描
        services.AddScoped<IDocumentProcessEnqueue, InlineDocumentProcessEnqueue>();
        return services;
    }
}

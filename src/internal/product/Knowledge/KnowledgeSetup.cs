using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.Product.Knowledge;

public static class KnowledgeSetup
{
    public static IServiceCollection AddSailNoraKnowledge(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration;
        // DocumentProcessJob / KnowledgeService / LocalFileStorage / GrpcDocReaderClient 由 [Scoped]/[Singleton] 源生成注册
        // Hangfire.Enabled=true 时由 App.HangfireSetup 覆盖为 HangfireDocumentProcessEnqueue
        services.AddScoped<IDocumentProcessEnqueue, InlineDocumentProcessEnqueue>();
        return services;
    }
}

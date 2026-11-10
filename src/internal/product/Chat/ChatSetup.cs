using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SailNora.Product.Ai;
using SailNora.Product.Chat.Rerank;

namespace SailNora.Product.Chat;

public static class ChatSetup
{
    public static IServiceCollection AddSailNoraChat(this IServiceCollection services)
    {
        services.AddHttpClient("rerank", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("sail-nora-rerank/0.1");
        });
        services.AddSingleton<LexicalReranker>();
        services.AddSingleton<HttpReranker>();
        services.AddSingleton<IReranker>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            if (string.Equals(opt.RerankProvider, "http", StringComparison.OrdinalIgnoreCase))
                return sp.GetRequiredService<HttpReranker>();
            return sp.GetRequiredService<LexicalReranker>();
        });
        return services;
    }
}

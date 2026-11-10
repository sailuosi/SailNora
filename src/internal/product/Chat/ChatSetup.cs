using Microsoft.Extensions.DependencyInjection;
using SailNora.Product.Chat.Rerank;

namespace SailNora.Product.Chat;

public static class ChatSetup
{
    public static IServiceCollection AddSailNoraChat(this IServiceCollection services)
    {
        // LexicalReranker 亦由 Scrutor 扫描；此处显式兜底
        services.AddSingleton<IReranker, LexicalReranker>();
        return services;
    }
}

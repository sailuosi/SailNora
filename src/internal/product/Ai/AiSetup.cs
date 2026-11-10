using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SailNora.Product.Ai.Embedding;
using SailNora.Product.Knowledge.Vector;

namespace SailNora.Product.Ai;

public static class AiSetup
{
    public static IServiceCollection AddSailNoraAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddSingleton<PlaceholderChatClient>();
        services.AddSingleton<AIAgent>(CreateAgent);
        services.AddSingleton<ITextEmbedder>(CreateEmbedder);
        services.AddScoped<JsonChunkVectorIndex>();
        services.AddScoped<PgVectorChunkIndex>();
        services.AddScoped<IChunkVectorIndex>(CreateVectorIndex);
        return services;
    }

    private static IChunkVectorIndex CreateVectorIndex(IServiceProvider sp)
    {
        var opt = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        if (string.Equals(opt.VectorStore, VectorStoreKinds.Pgvector, StringComparison.OrdinalIgnoreCase))
            return sp.GetRequiredService<PgVectorChunkIndex>();
        return sp.GetRequiredService<JsonChunkVectorIndex>();
    }

    private static ITextEmbedder CreateEmbedder(IServiceProvider sp)
    {
        var opt = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        var useOpenAi = string.Equals(opt.EmbeddingProvider, "openai", StringComparison.OrdinalIgnoreCase)
                        || (string.Equals(opt.Provider, AiProviders.OpenAI, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(opt.EmbeddingProvider, "auto", StringComparison.OrdinalIgnoreCase));

        if (useOpenAi && !string.IsNullOrWhiteSpace(opt.ApiKey))
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiTextEmbedder>>();
            return new OpenAiTextEmbedder(opt, logger);
        }

        return new HashingEmbeddingGenerator(opt.EmbeddingDimensions);
    }

    private static AIAgent CreateAgent(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<AiOptions>>().Value;
        var instructions = string.IsNullOrWhiteSpace(options.Instructions)
            ? "你是 sail-nora 知识助手，请用简洁、准确的中文回答。"
            : options.Instructions.Trim();
        var name = string.IsNullOrWhiteSpace(options.AgentName) ? "sail-nora助手" : options.AgentName.Trim();

        if (string.Equals(options.Provider, AiProviders.OpenAI, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            IChatClient chatClient = SailNoraAiClient.CreateOpenAiChatClient(options)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
            return chatClient.AsAIAgent(instructions: instructions, name: name);
        }

        var placeholder = sp.GetRequiredService<PlaceholderChatClient>();
        IChatClient withTools = placeholder.AsBuilder().UseFunctionInvocation().Build();
        return withTools.AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions }
        });
    }
}

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace SailNora.Product.Ai;

public static class AiSetup
{
    public static IServiceCollection AddSailNoraAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddSingleton<PlaceholderChatClient>();
        services.AddSingleton<AIAgent>(CreateAgent);
        return services;
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
            var chatClient = SailNoraAiClient.CreateOpenAiChatClient(options);
            return chatClient.AsAIAgent(instructions: instructions, name: name);
        }

        var placeholder = sp.GetRequiredService<PlaceholderChatClient>();
        return placeholder.AsAIAgent(new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions }
        });
    }
}

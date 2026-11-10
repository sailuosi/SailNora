using System.ClientModel;
using OpenAI;
using OpenAI.Chat;

namespace SailNora.Product.Ai;

internal static class SailNoraAiClient
{
    internal static ChatClient CreateOpenAiChatClient(AiOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new InvalidOperationException("Ai:ApiKey is required for OpenAI provider.");

        var openAiClient = string.IsNullOrWhiteSpace(options.Endpoint)
            ? new OpenAIClient(options.ApiKey)
            : new OpenAIClient(
                new ApiKeyCredential(options.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) });

        return openAiClient.GetChatClient(options.Model);
    }
}

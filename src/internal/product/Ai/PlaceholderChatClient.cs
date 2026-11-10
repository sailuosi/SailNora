using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;

namespace SailNora.Product.Ai;

public sealed class PlaceholderChatClient : IChatClient, ISingleton
{
    public ChatClientMetadata Metadata { get; } = new("placeholder");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var reply = "sail-nora AI placeholder: set Ai:Provider=OpenAI and ApiKey to enable a real model.";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const string text = "sail-nora AI streaming placeholder. Configure Ai section to connect a real model.";
        foreach (var chunk in text.Split(' '))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk + " ");
            await Task.Delay(20, cancellationToken);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

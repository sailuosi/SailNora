using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;
using SailNora.Product.Ai.Dtos;

namespace SailNora.Product.Ai.Service;

[Scoped]
public sealed class AiChatService
{
    private readonly AIAgent _agent;

    public AiChatService(AIAgent agent) => _agent = agent;

    public async IAsyncEnumerable<string> StreamChatAsync(
        AiChatInput input,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(input);
        if (messages.Count == 0)
            yield break;

        await foreach (var update in _agent.RunStreamingAsync(messages, session: null, cancellationToken: cancellationToken))
        {
            if (update.Text is { Length: > 0 } text)
                yield return text;
        }
    }

    private static List<ChatMessage> BuildMessages(AiChatInput input)
    {
        if (input.Messages is { Count: > 0 })
        {
            return input.Messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => new ChatMessage(ParseRole(m.Role), m.Content.Trim()))
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(input.Prompt))
            return [];

        return [new ChatMessage(ChatRole.User, input.Prompt.Trim())];
    }

    private static ChatRole ParseRole(string role) => role.ToLowerInvariant() switch
    {
        "assistant" => ChatRole.Assistant,
        "system" => ChatRole.System,
        _ => ChatRole.User
    };
}

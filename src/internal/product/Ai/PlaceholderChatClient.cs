using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;

namespace SailNora.Product.Ai;

/// <summary>
/// 无真实模型时的占位 ChatClient；若带 Tools，则模拟一次 function-call 以便冒烟原生 FC 路径。
/// </summary>
public sealed class PlaceholderChatClient : IChatClient, ISingleton
{
    public ChatClientMetadata Metadata { get; } = new("placeholder");

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        if (TryBuildToolCall(list, options) is { } call)
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));

        var reply = HasToolResults(list)
            ? "sail-nora Agent Placeholder：已根据工具结果生成回答（配置 Ai:Provider=OpenAI 与 ApiKey 可接真实模型）。"
            : "sail-nora AI placeholder: set Ai:Provider=OpenAI and ApiKey to enable a real model.";
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        if (TryBuildToolCall(list, options) is { } call)
        {
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [call]
            };
            yield break;
        }

        var text = HasToolResults(list)
            ? "sail-nora Agent Placeholder：已根据工具结果生成回答。"
            : "sail-nora AI streaming placeholder. Configure Ai section to connect a real model.";
        foreach (var chunk in text.Split(' '))
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk + " ");
            await Task.Delay(15, cancellationToken);
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private static bool HasToolResults(IList<ChatMessage> messages) =>
        messages.Any(m => m.Contents.OfType<FunctionResultContent>().Any());

    private static FunctionCallContent? TryBuildToolCall(IList<ChatMessage> messages, ChatOptions? options)
    {
        if (options?.Tools is not { Count: > 0 } tools)
            return null;
        if (HasToolResults(messages))
            return null;
        if (messages.Any(m => m.Contents.OfType<FunctionCallContent>().Any()))
            return null;

        var query = ExtractLastUserText(messages) ?? "knowledge";
        var functions = tools.OfType<AIFunction>().ToList();
        var shell = functions.FirstOrDefault(t => t.Name == "shell_exec");
        var preferShell = shell is not null && LooksLikeShellRequest(query);
        var fn = preferShell
            ? shell
            : functions.FirstOrDefault(t => t.Name == "search_knowledge") ?? functions.FirstOrDefault();
        if (fn is null)
            return null;

        IDictionary<string, object?> args = fn.Name switch
        {
            "search_knowledge" => new Dictionary<string, object?> { ["query"] = query },
            "list_documents" => new Dictionary<string, object?>(),
            "read_document" => new Dictionary<string, object?> { ["knowledge_id"] = "" },
            "shell_exec" => new Dictionary<string, object?> { ["command"] = "echo sail-nora-sandbox" },
            _ => new Dictionary<string, object?>()
        };

        return new FunctionCallContent($"ph_{Guid.NewGuid():N}"[..16], fn.Name, args);
    }

    private static bool LooksLikeShellRequest(string query) =>
        query.Contains("shell", StringComparison.OrdinalIgnoreCase)
        || query.Contains("echo", StringComparison.OrdinalIgnoreCase)
        || query.Contains("执行", StringComparison.Ordinal)
        || query.Contains("命令", StringComparison.Ordinal);

    private static string? ExtractLastUserText(IList<ChatMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            if (messages[i].Role != ChatRole.User)
                continue;
            var text = messages[i].Text;
            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        return null;
    }
}

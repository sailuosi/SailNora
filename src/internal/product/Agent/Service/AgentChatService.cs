using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;
using SailNora.Product.Agent.Dtos;
using SailNora.Product.Agent.Tools;
using SailNora.Product.Chat.Dtos;
using SailNora.Product.Chat.Service;

namespace SailNora.Product.Agent.Service;

/// <summary>
/// F5 最小 Agent：编排工具事件（Placeholder 无原生 function-call 时仍保证 tool_call/tool_result SSE）。
/// </summary>
public sealed class AgentChatService : IScoped
{
    private readonly SessionService _sessions;
    private readonly AgentService _agents;
    private readonly KnowledgeAgentTools _tools;
    private readonly AIAgent _agent;

    public AgentChatService(
        SessionService sessions,
        AgentService agents,
        KnowledgeAgentTools tools,
        AIAgent agent)
    {
        _sessions = sessions;
        _agents = agents;
        _tools = tools;
        _agent = agent;
    }

    public async IAsyncEnumerable<StreamResponseDto> StreamAsync(
        Guid spaceId,
        Guid userId,
        string sessionId,
        AgentChatInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = _sessions.GetOwned(spaceId, userId, sessionId)
                      ?? throw new InvalidOperationException("会话不存在");
        if (string.IsNullOrWhiteSpace(input.Query))
            throw new InvalidOperationException("query 不能为空");

        var history = _sessions.ListMessages(sessionId);
        yield return Evt("load_history", new
        {
            messages = history.Select(m => new { id = m.Id, role = m.Role, content = m.Content }).ToList()
        });
        yield return Evt("session_title", new { title = session.Title });

        string? agentName = null;
        string instructions = "你是 sail-nora Agent。先检索知识再回答，并引用片段编号。";
        if (!string.IsNullOrWhiteSpace(input.AgentId))
        {
            var custom = _agents.GetOwned(spaceId, input.AgentId)
                         ?? throw new InvalidOperationException("Agent 不存在");
            agentName = custom.Name;
            if (!string.IsNullOrWhiteSpace(custom.Instructions))
                instructions = custom.Instructions!;
            yield return Evt("agent_query", new { agent_id = custom.Id, agent_name = custom.Name });
        }

        yield return Evt("thinking", new { content = "正在规划工具调用…" });

        // --- search_knowledge ---
        var searchArgs = new
        {
            query = input.Query.Trim(),
            knowledge_base_ids = input.KnowledgeBaseIds,
            knowledge_ids = input.KnowledgeIds
        };
        yield return Evt("tool_call", new
        {
            tool_call_id = "tc_search",
            name = "search_knowledge",
            arguments = searchArgs
        });

        var searchResult = await _tools.SearchKnowledgeAsync(
            spaceId,
            input.Query,
            input.KnowledgeBaseIds,
            input.KnowledgeIds,
            topK: 5,
            cancellationToken);
        yield return Evt("tool_result", new
        {
            tool_call_id = "tc_search",
            name = "search_knowledge",
            success = true,
            result = searchResult
        });

        // --- list_documents（查询含「列表/有哪些文档」时） ---
        var q = input.Query;
        if (q.Contains("列表", StringComparison.Ordinal) || q.Contains("有哪些", StringComparison.Ordinal)
            || q.Contains("list", StringComparison.OrdinalIgnoreCase))
        {
            var kbId = input.KnowledgeBaseIds?.FirstOrDefault();
            yield return Evt("tool_call", new
            {
                tool_call_id = "tc_list",
                name = "list_documents",
                arguments = new { knowledge_base_id = kbId }
            });
            var listResult = _tools.ListDocuments(spaceId, kbId);
            yield return Evt("tool_result", new
            {
                tool_call_id = "tc_list",
                name = "list_documents",
                success = true,
                result = listResult
            });
        }

        _sessions.AddMessage(spaceId, sessionId, "user", input.Query.Trim());

        var toolJson = KnowledgeAgentTools.ToJson(searchResult);
        var prompt =
            $"{instructions}\n\n工具 search_knowledge 结果：\n{toolJson}\n\n用户问题：{input.Query.Trim()}\n请基于工具结果作答。";

        var messages = new List<ChatMessage>();
        foreach (var m in history.TakeLast(6))
            messages.Add(new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Content));
        messages.Add(new ChatMessage(ChatRole.User, prompt));

        var answer = new System.Text.StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(messages, session: null, cancellationToken: cancellationToken))
        {
            if (update.Text is not { Length: > 0 } text)
                continue;
            answer.Append(text);
            yield return Evt("answer", new { content = text });
        }

        if (answer.Length == 0)
        {
            var fallback = agentName is null
                ? "（Agent Placeholder 输出；配置 Ai.ApiKey 可接真实模型。）"
                : $"（Agent「{agentName}」暂无模型输出。）";
            answer.Append(fallback);
            yield return Evt("answer", new { content = fallback });
        }

        _sessions.AddMessage(spaceId, sessionId, "assistant", answer.ToString());
        yield return Evt("complete", new { }, done: true);
    }

    private static StreamResponseDto Evt(string type, object data, bool done = false) => new()
    {
        ResponseType = type,
        Data = data,
        Done = done
    };
}

using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using SailNora.Foundation.DI;
using SailNora.Product.Agent.Dtos;
using SailNora.Product.Agent.Tools;
using SailNora.Product.Ai;
using SailNora.Product.Chat.DBLayer;
using SailNora.Product.Chat.Dtos;
using SailNora.Product.Chat.Service;
using SailNora.Product.Mcp.DBLayer;
using SailNora.Product.Mcp.Service;

namespace SailNora.Product.Agent.Service;

/// <summary>
/// F5/F5b/F5c Agent：原生 FC + 编排回退 + 可选 HITL 审批事件。
/// </summary>
public sealed class AgentChatService : IScoped
{
    private readonly SessionService _sessions;
    private readonly AgentService _agents;
    private readonly KnowledgeAgentTools _tools;
    private readonly AgentNativeToolFactory _toolFactory;
    private readonly HitlService _hitl;
    private readonly AIAgent _agent;
    private readonly AiOptions _aiOptions;

    public AgentChatService(
        SessionService sessions,
        AgentService agents,
        KnowledgeAgentTools tools,
        AgentNativeToolFactory toolFactory,
        HitlService hitl,
        AIAgent agent,
        IOptions<AiOptions> aiOptions)
    {
        _sessions = sessions;
        _agents = agents;
        _tools = tools;
        _toolFactory = toolFactory;
        _hitl = hitl;
        _agent = agent;
        _aiOptions = aiOptions.Value;
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

        var useNative = string.Equals(
            _aiOptions.AgentToolMode, AgentToolModes.Native, StringComparison.OrdinalIgnoreCase);

        if (useNative)
        {
            await foreach (var evt in StreamNativeAsync(
                               spaceId, sessionId, input, history, instructions, agentName, cancellationToken))
                yield return evt;
        }
        else
        {
            await foreach (var evt in StreamOrchestratedAsync(
                               spaceId, sessionId, input, history, instructions, agentName, cancellationToken))
                yield return evt;
        }
    }

    private async IAsyncEnumerable<StreamResponseDto> StreamNativeAsync(
        Guid spaceId,
        string sessionId,
        AgentChatInput input,
        IReadOnlyList<KnMessage> history,
        string instructions,
        string? agentName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return Evt("thinking", new { content = "原生工具调用中…", mode = "native" });

        await foreach (var hitlEvt in EmitHitlIfNeededAsync(
                           spaceId, sessionId, "search_knowledge",
                           new { query = input.Query.Trim() }, cancellationToken))
            yield return hitlEvt;

        var tools = _toolFactory.Create(spaceId, input.KnowledgeBaseIds, input.KnowledgeIds);
        _sessions.AddMessage(spaceId, sessionId, "user", input.Query.Trim());

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, instructions)
        };
        foreach (var m in history.TakeLast(6))
            messages.Add(new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Content));
        messages.Add(new ChatMessage(ChatRole.User, input.Query.Trim()));

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            Tools = tools
        });

        var callNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var answer = new System.Text.StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(
                           messages, session: null, options: runOptions, cancellationToken: cancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent call:
                        callNames[call.CallId] = call.Name;
                        yield return Evt("tool_call", new
                        {
                            tool_call_id = call.CallId,
                            name = call.Name,
                            arguments = call.Arguments
                        });
                        break;
                    case FunctionResultContent result:
                        callNames.TryGetValue(result.CallId, out var toolName);
                        yield return Evt("tool_result", new
                        {
                            tool_call_id = result.CallId,
                            name = toolName,
                            success = result.Exception is null,
                            result = result.Result
                        });
                        break;
                    case TextContent text when text.Text is { Length: > 0 }:
                        answer.Append(text.Text);
                        yield return Evt("answer", new { content = text.Text });
                        break;
                }
            }

            if (update.Contents.Count == 0 && update.Text is { Length: > 0 } plain)
            {
                answer.Append(plain);
                yield return Evt("answer", new { content = plain });
            }
        }

        if (answer.Length == 0)
        {
            var fallback = agentName is null
                ? "（Agent native Placeholder 无文本输出。）"
                : $"（Agent「{agentName}」暂无模型输出。）";
            answer.Append(fallback);
            yield return Evt("answer", new { content = fallback });
        }

        _sessions.AddMessage(spaceId, sessionId, "assistant", answer.ToString());
        yield return Evt("complete", new { mode = "native" }, done: true);
    }

    private async IAsyncEnumerable<StreamResponseDto> StreamOrchestratedAsync(
        Guid spaceId,
        string sessionId,
        AgentChatInput input,
        IReadOnlyList<KnMessage> history,
        string instructions,
        string? agentName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return Evt("thinking", new { content = "正在规划工具调用…", mode = "orchestrated" });

        var searchArgs = new
        {
            query = input.Query.Trim(),
            knowledge_base_ids = input.KnowledgeBaseIds,
            knowledge_ids = input.KnowledgeIds
        };

        await foreach (var hitlEvt in EmitHitlIfNeededAsync(
                           spaceId, sessionId, "search_knowledge", searchArgs, cancellationToken))
            yield return hitlEvt;

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
        yield return Evt("complete", new { mode = "orchestrated" }, done: true);
    }

    private async IAsyncEnumerable<StreamResponseDto> EmitHitlIfNeededAsync(
        Guid spaceId,
        string sessionId,
        string toolName,
        object arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_aiOptions.HitlEnabled)
            yield break;

        var pending = _hitl.CreatePending(spaceId, sessionId, toolName, arguments);
        yield return Evt("tool_approval_required", new
        {
            pending_id = pending.Id,
            tool_name = toolName,
            arguments,
            timeout_seconds = _aiOptions.HitlTimeoutSeconds
        });

        var decision = await _hitl.WaitForDecisionAsync(
            pending.Id,
            TimeSpan.FromSeconds(Math.Max(0, _aiOptions.HitlTimeoutSeconds)),
            cancellationToken);

        yield return Evt("tool_approval_resolved", new
        {
            pending_id = pending.Id,
            tool_name = toolName,
            status = decision
        });

        if (decision == HitlStatuses.Rejected)
            throw new InvalidOperationException($"工具 {toolName} 审批被拒绝");
    }

    private static StreamResponseDto Evt(string type, object data, bool done = false) => new()
    {
        ResponseType = type,
        Data = data,
        Done = done
    };
}

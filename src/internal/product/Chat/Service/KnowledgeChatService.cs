using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.Dtos;

namespace SailNora.Product.Chat.Service;

[Scoped]
public sealed class KnowledgeChatService
{
    private readonly SessionService _sessions;
    private readonly ChunkRetrievalService _retrieval;
    private readonly AIAgent _agent;

    public KnowledgeChatService(
        SessionService sessions,
        ChunkRetrievalService retrieval,
        AIAgent agent)
    {
        _sessions = sessions;
        _retrieval = retrieval;
        _agent = agent;
    }

    public async IAsyncEnumerable<StreamResponseDto> StreamAsync(
        Guid spaceId,
        Guid userId,
        string sessionId,
        KnowledgeChatInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = _sessions.GetOwned(spaceId, userId, sessionId)
                      ?? throw new InvalidOperationException("会话不存在");

        if (string.IsNullOrWhiteSpace(input.Query))
            throw new InvalidOperationException("query 不能为空");

        var history = _sessions.ListMessages(sessionId);
        yield return new StreamResponseDto
        {
            ResponseType = "load_history",
            Data = new
            {
                messages = history.Select(m => new
                {
                    id = m.Id,
                    role = m.Role,
                    content = m.Content,
                    create_time = m.CreateTime
                }).ToList()
            }
        };

        yield return new StreamResponseDto
        {
            ResponseType = "session_title",
            Data = new { title = session.Title }
        };

        var hits = await _retrieval.SearchAsync(
            spaceId,
            input.Query,
            input.KnowledgeBaseIds,
            input.KnowledgeIds,
            topK: 5,
            cancellationToken);

        yield return new StreamResponseDto
        {
            ResponseType = "references",
            Data = new
            {
                items = hits.Select(h => new
                {
                    knowledge_id = h.KnowledgeId,
                    knowledge_base_id = h.KnowledgeBaseId,
                    chunk_id = h.Id,
                    content = h.Content.Length > 240 ? h.Content[..240] : h.Content,
                    score = Math.Round(h.Score, 4),
                    keyword_score = Math.Round(h.KeywordScore, 4),
                    vector_score = Math.Round(h.VectorScore, 4)
                }).ToList()
            }
        };

        _sessions.AddMessage(spaceId, sessionId, "user", input.Query.Trim());

        var context = string.Join("\n\n---\n\n", hits.Select((h, i) => $"[{i + 1}] {h.Content}"));
        var prompt = string.IsNullOrWhiteSpace(context)
            ? input.Query.Trim()
            : $"基于以下知识片段回答用户问题。若无法从片段得出答案，请明确说明。\n\n{context}\n\n问题：{input.Query.Trim()}";

        var messages = new List<ChatMessage>();
        foreach (var m in history.TakeLast(8))
            messages.Add(new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Content));
        messages.Add(new ChatMessage(ChatRole.User, prompt));

        var answer = new System.Text.StringBuilder();
        await foreach (var update in _agent.RunStreamingAsync(messages, session: null, cancellationToken: cancellationToken))
        {
            if (update.Text is not { Length: > 0 } text)
                continue;
            answer.Append(text);
            yield return new StreamResponseDto
            {
                ResponseType = "answer",
                Data = new { content = text }
            };
        }

        if (answer.Length == 0)
        {
            const string fallback = "（暂无模型输出；请配置 Ai.ApiKey 或检查 Placeholder。）";
            answer.Append(fallback);
            yield return new StreamResponseDto
            {
                ResponseType = "answer",
                Data = new { content = fallback }
            };
        }

        _sessions.AddMessage(spaceId, sessionId, "assistant", answer.ToString());

        yield return new StreamResponseDto
        {
            ResponseType = "complete",
            Done = true,
            Data = new { }
        };
    }
}

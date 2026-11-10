using System.Text.Json;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.Service;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Service;
using SailNora.Foundation.Data;

namespace SailNora.Product.Agent.Tools;

/// <summary>对齐上游 Agent 核心工具名：search_knowledge / list_documents / read_document。</summary>
[Scoped]
public sealed class KnowledgeAgentTools
{
    private readonly ChunkRetrievalService _retrieval;
    private readonly KnowledgeService _knowledge;
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly MooRepository<KnChunk> _chunks;

    public KnowledgeAgentTools(
        ChunkRetrievalService retrieval,
        KnowledgeService knowledge,
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks)
    {
        _retrieval = retrieval;
        _knowledge = knowledge;
        _knowledges = knowledges;
        _chunks = chunks;
    }

    public async Task<object> SearchKnowledgeAsync(
        Guid spaceId,
        string query,
        IEnumerable<string>? knowledgeBaseIds,
        IEnumerable<string>? knowledgeIds,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var hits = await _retrieval.SearchAsync(spaceId, query, knowledgeBaseIds, knowledgeIds, topK, cancellationToken);
        return new
        {
            tool = "search_knowledge",
            success = true,
            items = hits.Select(h => new
            {
                chunk_id = h.Id,
                knowledge_id = h.KnowledgeId,
                knowledge_base_id = h.KnowledgeBaseId,
                content = h.Content.Length > 400 ? h.Content[..400] : h.Content,
                score = Math.Round(h.Score, 4)
            }).ToList()
        };
    }

    public object ListDocuments(Guid spaceId, string? knowledgeBaseId)
    {
        IEnumerable<KnKnowledge> rows = _knowledges.Repo.GetList(k => k.SpaceId == spaceId);
        if (!string.IsNullOrWhiteSpace(knowledgeBaseId))
            rows = rows.Where(k => k.KnowledgeBaseId == knowledgeBaseId);

        var items = rows
            .OrderByDescending(k => k.CreateTime)
            .Take(50)
            .Select(k => new
            {
                knowledge_id = k.Id,
                knowledge_base_id = k.KnowledgeBaseId,
                title = k.Title,
                file_name = k.FileName,
                parse_status = k.ParseStatus
            })
            .ToList();

        return new { tool = "list_documents", success = true, items };
    }

    public object ReadDocument(Guid spaceId, string knowledgeId, int maxChunks = 8)
    {
        var doc = _knowledge.GetKnowledge(spaceId, knowledgeId);
        if (doc is null)
            return new { tool = "read_document", success = false, error = "知识不存在" };

        var chunks = _chunks.Repo.GetList(c => c.KnowledgeId == knowledgeId && c.SpaceId == spaceId)
            .OrderBy(c => c.ChunkIndex)
            .Take(Math.Clamp(maxChunks, 1, 50))
            .Select(c => new { chunk_index = c.ChunkIndex, content = c.Content })
            .ToList();

        return new
        {
            tool = "read_document",
            success = true,
            knowledge_id = knowledgeId,
            title = doc.Title,
            chunks
        };
    }

    public static string ToJson(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
}

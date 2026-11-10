using Microsoft.Extensions.Options;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Ai;
using SailNora.Product.Ai.Embedding;
using SailNora.Product.Chat.Rerank;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Vector;

namespace SailNora.Product.Chat.Service;

public sealed class RetrievalHitDto
{
    public string Id { get; set; } = string.Empty;
    public string KnowledgeId { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double KeywordScore { get; set; }
    public double VectorScore { get; set; }
    public double Score { get; set; }
}

public sealed class ChunkRetrievalService : IScoped
{
    private readonly MooRepository<KnChunk> _chunks;
    private readonly ITextEmbedder _embeddings;
    private readonly IChunkVectorIndex _vectorIndex;
    private readonly IReranker _reranker;
    private readonly AiOptions _aiOptions;

    public ChunkRetrievalService(
        MooRepository<KnChunk> chunks,
        ITextEmbedder embeddings,
        IChunkVectorIndex vectorIndex,
        IReranker reranker,
        IOptions<AiOptions> aiOptions)
    {
        _chunks = chunks;
        _embeddings = embeddings;
        _vectorIndex = vectorIndex;
        _reranker = reranker;
        _aiOptions = aiOptions.Value;
    }

    public async Task<List<RetrievalHitDto>> SearchAsync(
        Guid spaceId,
        string query,
        IEnumerable<string>? knowledgeBaseIds,
        IEnumerable<string>? knowledgeIds,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var kbIds = knowledgeBaseIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                    ?? [];
        var kidSet = knowledgeIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase)
                     ?? [];

        IEnumerable<KnChunk> rows = _chunks.Repo.GetList(c => c.SpaceId == spaceId);
        if (kbIds.Count > 0)
            rows = rows.Where(c => kbIds.Contains(c.KnowledgeBaseId));
        if (kidSet.Count > 0)
            rows = rows.Where(c => kidSet.Contains(c.KnowledgeId));

        var material = rows.ToList();
        var q = query.Trim();
        float[]? qVec = null;
        if (!string.IsNullOrWhiteSpace(q))
            qVec = await _embeddings.EmbedAsync(q, cancellationToken);

        IReadOnlyDictionary<string, double> vectorScores = new Dictionary<string, double>();
        if (qVec is not null)
        {
            vectorScores = await _vectorIndex.ScoreAsync(
                qVec,
                material.Select(c => (c.Id, c.EmbeddingJson)),
                cancellationToken);
        }

        var candidateLimit = Math.Clamp(topK * 4, topK, 50);
        var hits = new List<RetrievalHitDto>();
        foreach (var c in material)
        {
            var keyword = KeywordScore(c.Content, q);
            vectorScores.TryGetValue(c.Id, out var vector);
            var score = keyword * 1.0 + Math.Max(0, vector) * 2.0;
            hits.Add(new RetrievalHitDto
            {
                Id = c.Id,
                KnowledgeId = c.KnowledgeId,
                KnowledgeBaseId = c.KnowledgeBaseId,
                Content = c.Content,
                KeywordScore = keyword,
                VectorScore = vector,
                Score = score
            });
        }

        hits = hits.OrderByDescending(h => h.Score).ToList();
        if (kbIds.Count == 0 && kidSet.Count == 0)
            hits = hits.Where(h => h.Score > 0).ToList();

        hits = hits.Take(candidateLimit).ToList();
        if (_aiOptions.RerankEnabled)
            return _reranker.Rerank(q, hits, topK).ToList();

        return hits.Take(Math.Clamp(topK, 1, 50)).ToList();
    }

    private static double KeywordScore(string content, string query)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(query))
            return 0;
        var hay = content.ToLowerInvariant();
        var terms = query.ToLowerInvariant().Split(
            [' ', '\t', '\n', '，', '。', '？', '?'],
            StringSplitOptions.RemoveEmptyEntries);
        var hits = terms.Count(t => hay.Contains(t, StringComparison.Ordinal));
        return hits + (hay.Contains(query.ToLowerInvariant(), StringComparison.Ordinal) ? 2 : 0);
    }
}

using Microsoft.Extensions.Logging;

namespace SailNora.Product.Knowledge.Vector;

/// <summary>
/// F3 占位：pgvector 未接线时回退 EmbeddingJson 余弦，并打日志。
/// 真 Postgres + halfvec/HNSW 留待迁移与连接串就绪后替换。
/// </summary>
public sealed class PgVectorChunkIndex : IChunkVectorIndex
{
    private readonly JsonChunkVectorIndex _fallback;
    private readonly ILogger<PgVectorChunkIndex> _logger;
    private int _warned;

    public PgVectorChunkIndex(JsonChunkVectorIndex fallback, ILogger<PgVectorChunkIndex> logger)
    {
        _fallback = fallback;
        _logger = logger;
    }

    public string Kind => VectorStoreKinds.Pgvector;

    public Task<IReadOnlyDictionary<string, double>> ScoreAsync(
        float[] queryVector,
        IEnumerable<(string ChunkId, string? EmbeddingJson)> candidates,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _warned, 1) == 0)
            _logger.LogWarning("Ai:VectorStore=pgvector but PgVector index not wired; falling back to EmbeddingJson cosine");

        return _fallback.ScoreAsync(queryVector, candidates, cancellationToken);
    }
}

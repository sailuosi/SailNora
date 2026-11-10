namespace SailNora.Product.Knowledge.Vector;

/// <summary>分块向量索引抽象：Lite 默认 json；可选 pgvector。</summary>
public interface IChunkVectorIndex
{
    string Kind { get; }

    /// <summary>对候选分块打向量相似度分（chunkId → cosine）。</summary>
    Task<IReadOnlyDictionary<string, double>> ScoreAsync(
        float[] queryVector,
        IEnumerable<(string ChunkId, string? EmbeddingJson)> candidates,
        CancellationToken cancellationToken = default);
}

public static class VectorStoreKinds
{
    public const string Json = "json";
    public const string Pgvector = "pgvector";
}

using SailNora.Product.Ai.Embedding;

namespace SailNora.Product.Knowledge.Vector;

/// <summary>从 KnChunk.EmbeddingJson 做应用层余弦（Lite / 默认）。</summary>
public sealed class JsonChunkVectorIndex : IChunkVectorIndex
{
    public string Kind => VectorStoreKinds.Json;

    public Task<IReadOnlyDictionary<string, double>> ScoreAsync(
        float[] queryVector,
        IEnumerable<(string ChunkId, string? EmbeddingJson)> candidates,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var map = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (chunkId, embeddingJson) in candidates)
        {
            var emb = EmbeddingCodec.FromJson(embeddingJson);
            if (emb is null)
                continue;
            map[chunkId] = HashingEmbeddingGenerator.Cosine(queryVector, emb);
        }

        return Task.FromResult<IReadOnlyDictionary<string, double>>(map);
    }
}

using System.Security.Cryptography;
using System.Text;

namespace SailNora.Product.Ai.Embedding;

/// <summary>
/// 本地确定性伪向量（无外网依赖），便于 SQLite 阶段混合检索冒烟。
/// 正式环境应切 OpenAI/兼容 Embedding API。
/// </summary>
public sealed class HashingEmbeddingGenerator : ITextEmbedder
{
    public string ProviderName => "hashing";

    public int Dimensions { get; }

    public HashingEmbeddingGenerator(int dimensions = 64) =>
        Dimensions = dimensions <= 0 ? 64 : dimensions;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Embed(text));
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var list = new List<float[]>(texts.Count);
        foreach (var t in texts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            list.Add(await EmbedAsync(t, cancellationToken));
        }

        return list;
    }

    private float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        if (string.IsNullOrWhiteSpace(text))
            return vector;

        var tokens = text.ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', '，', '。', '、', '？', '?', '！', '!'],
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            for (var i = 0; i < Dimensions; i++)
            {
                var b = hash[i % hash.Length];
                vector[i] += ((b / 255f) * 2f - 1f) / MathF.Sqrt(Math.Max(1, tokens.Length));
            }
        }

        return Normalize(vector);
    }

    public static float[] Normalize(float[] v)
    {
        double sum = 0;
        for (var i = 0; i < v.Length; i++)
            sum += v[i] * v[i];
        var norm = Math.Sqrt(sum);
        if (norm < 1e-8)
            return v;
        for (var i = 0; i < v.Length; i++)
            v[i] = (float)(v[i] / norm);
        return v;
    }

    public static double Cosine(float[] a, float[] b)
    {
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }

        if (na < 1e-12 || nb < 1e-12)
            return 0;
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}

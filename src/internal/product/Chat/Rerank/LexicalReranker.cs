using SailNora.Foundation.DI;
using SailNora.Product.Chat.Service;

namespace SailNora.Product.Chat.Rerank;

/// <summary>
/// 轻量词项重排：在混合分基础上按查询词覆盖率微调，无需外部 Rerank 模型。
/// </summary>
[Singleton]
public sealed class LexicalReranker : IReranker
{
    public string Name => "lexical";

    public IReadOnlyList<RetrievalHitDto> Rerank(string query, IReadOnlyList<RetrievalHitDto> hits, int topK)
    {
        if (hits.Count == 0)
            return hits;

        var terms = (query ?? string.Empty).ToLowerInvariant()
            .Split([' ', '\t', '\n', '，', '。', '？', '?', '、'], StringSplitOptions.RemoveEmptyEntries);

        var ranked = hits
            .Select(h =>
            {
                var coverage = Coverage(h.Content, terms);
                h.Score = h.Score + coverage * 0.5;
                return h;
            })
            .OrderByDescending(h => h.Score)
            .ThenByDescending(h => h.VectorScore)
            .Take(Math.Clamp(topK, 1, 50))
            .ToList();

        return ranked;
    }

    private static double Coverage(string content, string[] terms)
    {
        if (terms.Length == 0 || string.IsNullOrWhiteSpace(content))
            return 0;
        var hay = content.ToLowerInvariant();
        var hit = terms.Count(t => hay.Contains(t, StringComparison.Ordinal));
        return (double)hit / terms.Length;
    }
}

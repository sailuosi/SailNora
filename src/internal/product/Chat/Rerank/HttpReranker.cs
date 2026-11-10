using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SailNora.Product.Ai;
using SailNora.Product.Chat.Service;

namespace SailNora.Product.Chat.Rerank;

/// <summary>
/// F3：外部 HTTP Rerank（Cohere 风格 results[] 或简单 scores[]）。
/// 未配置 Endpoint / 调用失败时回退 <see cref="LexicalReranker"/>。
/// </summary>
public sealed class HttpReranker : IReranker
{
    private readonly AiOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LexicalReranker _fallback;
    private readonly ILogger<HttpReranker> _logger;

    public HttpReranker(
        IOptions<AiOptions> options,
        IHttpClientFactory httpClientFactory,
        LexicalReranker fallback,
        ILogger<HttpReranker> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _fallback = fallback;
        _logger = logger;
    }

    public string Name => "http";

    public IReadOnlyList<RetrievalHitDto> Rerank(string query, IReadOnlyList<RetrievalHitDto> hits, int topK)
    {
        if (hits.Count == 0)
            return hits;
        if (string.IsNullOrWhiteSpace(_options.RerankEndpoint))
            return _fallback.Rerank(query, hits, topK);

        try
        {
            return RerankHttpAsync(query, hits, topK).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "external rerank failed; falling back to lexical");
            return _fallback.Rerank(query, hits, topK);
        }
    }

    private async Task<IReadOnlyList<RetrievalHitDto>> RerankHttpAsync(
        string query,
        IReadOnlyList<RetrievalHitDto> hits,
        int topK)
    {
        var client = _httpClientFactory.CreateClient("rerank");
        var docs = hits.Select(h => h.Content).ToList();
        var payload = new
        {
            model = string.IsNullOrWhiteSpace(_options.RerankModel) ? "rerank-multilingual-v3.0" : _options.RerankModel,
            query,
            documents = docs,
            top_n = Math.Clamp(topK, 1, hits.Count)
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.RerankEndpoint!.Trim());
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        if (!string.IsNullOrWhiteSpace(_options.RerankApiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.RerankApiKey.Trim());

        using var resp = await client.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"rerank HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var root = doc.RootElement;
        var scored = new List<(int Index, double Score)>();

        if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in results.EnumerateArray())
            {
                var idx = r.TryGetProperty("index", out var i) && i.TryGetInt32(out var n) ? n : -1;
                var score = r.TryGetProperty("relevance_score", out var s) ? s.GetDouble()
                    : r.TryGetProperty("score", out var s2) ? s2.GetDouble() : 0d;
                if (idx >= 0 && idx < hits.Count)
                    scored.Add((idx, score));
            }
        }
        else if (root.TryGetProperty("scores", out var scores) && scores.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var s in scores.EnumerateArray())
            {
                if (i >= hits.Count) break;
                scored.Add((i, s.GetDouble()));
                i++;
            }
        }

        if (scored.Count == 0)
            return _fallback.Rerank(query, hits, topK);

        return scored
            .OrderByDescending(x => x.Score)
            .Select(x =>
            {
                var h = hits[x.Index];
                h.Score = x.Score;
                return h;
            })
            .Take(Math.Clamp(topK, 1, 50))
            .ToList();
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "…";
}

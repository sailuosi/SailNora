using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SailNora.Product.Chat.Controller;

/// <summary>本地冒烟用外部 Rerank 样例（Cohere 风格 results）。</summary>
[ApiController]
[AllowAnonymous]
public sealed class RerankSampleController : ControllerBase
{
    [HttpPost("api/v1/rerank/sample")]
    public async Task<IActionResult> Sample(CancellationToken cancellationToken)
    {
        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var query = root.TryGetProperty("query", out var q) ? q.GetString() ?? "" : "";
        if (!root.TryGetProperty("documents", out var documents) || documents.ValueKind != JsonValueKind.Array)
            return BadRequest(new { error = "documents required" });

        var terms = query.ToLowerInvariant()
            .Split([' ', '\t', '\n', '，', '。', '？', '?'], StringSplitOptions.RemoveEmptyEntries);
        var scored = new List<(int Index, double Score)>();
        var idx = 0;
        foreach (var d in documents.EnumerateArray())
        {
            var text = d.GetString() ?? "";
            var hay = text.ToLowerInvariant();
            var hit = terms.Count(t => hay.Contains(t, StringComparison.Ordinal));
            var score = terms.Length == 0 ? 0 : (double)hit / terms.Length;
            scored.Add((idx, Math.Round(score, 4)));
            idx++;
        }

        return Ok(new
        {
            results = scored
                .OrderByDescending(x => x.Score)
                .Select(x => new { index = x.Index, relevance_score = x.Score })
                .ToList()
        });
    }
}

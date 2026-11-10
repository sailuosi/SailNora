using System.Text.Json;

namespace SailNora.Product.Datasource.Service;

public sealed class FeishuDocItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

/// <summary>解析飞书文档列表或 sample-feishu-docs JSON。</summary>
public static class FeishuDocParser
{
    public static List<FeishuDocItem> Parse(string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("docs", out var docs) ? docs
            : root.TryGetProperty("items", out var items) ? items
            : root.TryGetProperty("pages", out var pages) ? pages
            : default;

        if (arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<FeishuDocItem>();
        foreach (var el in arr.EnumerateArray())
        {
            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString()
                     : el.TryGetProperty("document_id", out var did) ? did.GetString()
                     : null;
            var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var body = el.TryGetProperty("body", out var b) && b.ValueKind == JsonValueKind.String
                ? b.GetString() ?? ""
                : el.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String
                    ? c.GetString() ?? ""
                    : "";

            if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(title))
                continue;

            list.Add(new FeishuDocItem
            {
                Id = string.IsNullOrWhiteSpace(id) ? title : id!,
                Title = string.IsNullOrWhiteSpace(title) ? id! : title,
                Body = body
            });
        }

        return list;
    }
}

using System.Text.Json;

namespace SailNora.Product.Datasource.Service;

public sealed class ConfluencePageItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

/// <summary>解析 Confluence 页面列表或 sample-confluence-pages JSON。</summary>
public static class ConfluencePageParser
{
    public static List<ConfluencePageItem> Parse(string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("pages", out var pages) ? pages
            : root.TryGetProperty("results", out var results) ? results
            : default;

        if (arr.ValueKind != JsonValueKind.Array)
            return [];

        var items = new List<ConfluencePageItem>();
        foreach (var el in arr.EnumerateArray())
        {
            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString()
                     : el.TryGetProperty("page_id", out var pid) ? pid.GetString()
                     : null;
            var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var body = "";
            if (el.TryGetProperty("body", out var b))
            {
                if (b.ValueKind == JsonValueKind.String)
                    body = b.GetString() ?? "";
                else if (b.TryGetProperty("storage", out var st) && st.TryGetProperty("value", out var v))
                    body = v.GetString() ?? "";
            }
            else if (el.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                body = c.GetString() ?? "";

            if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(title))
                continue;

            items.Add(new ConfluencePageItem
            {
                Id = string.IsNullOrWhiteSpace(id) ? title : id!,
                Title = string.IsNullOrWhiteSpace(title) ? id! : title,
                Body = body
            });
        }

        return items;
    }
}

using System.Text.Json;

namespace SailNora.Product.Datasource.Service;

public sealed class GitLabTreeItem
{
    public string Path { get; set; } = string.Empty;
    public string Guid { get; set; } = string.Empty;
    public string Type { get; set; } = "blob";
    public string? Content { get; set; }
}

/// <summary>解析 GitLab repository/tree JSON 或 sample-gitlab-tree 扩展格式。</summary>
public static class GitLabTreeParser
{
    public static List<GitLabTreeItem> Parse(string json)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
        var root = doc.RootElement;
        var arr = root.ValueKind == JsonValueKind.Array
            ? root
            : root.TryGetProperty("tree", out var t) ? t : default;

        if (arr.ValueKind != JsonValueKind.Array)
            return [];

        var items = new List<GitLabTreeItem>();
        foreach (var el in arr.EnumerateArray())
        {
            var type = el.TryGetProperty("type", out var ty) ? ty.GetString() ?? "blob" : "blob";
            if (!string.Equals(type, "blob", StringComparison.OrdinalIgnoreCase))
                continue;

            var path = el.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString()
                     : el.TryGetProperty("blob_id", out var bid) ? bid.GetString()
                     : path;
            var content = el.TryGetProperty("content", out var c) ? c.GetString() : null;

            items.Add(new GitLabTreeItem
            {
                Path = path.Trim(),
                Guid = string.IsNullOrWhiteSpace(id) ? path : id!,
                Type = "blob",
                Content = content
            });
        }

        return items;
    }
}

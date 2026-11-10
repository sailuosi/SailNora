using System.Text.RegularExpressions;

namespace SailNora.Product.Wiki.Service;

/// <summary>从 Wiki Markdown 抽取出链（[[slug]] / /wiki/slug）。</summary>
public static class WikiLinkExtractor
{
    private static readonly Regex WikiLinkBrackets = new(
        @"\[\[([^|\]]+)(?:\|[^\]]*)?\]\]",
        RegexOptions.Compiled);

    private static readonly Regex WikiPathLink = new(
        @"(?i)/wiki/([a-z0-9\u4e00-\u9fff\-_]+)",
        RegexOptions.Compiled);

    public static IReadOnlyList<string> ExtractOutLinks(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in WikiLinkBrackets.Matches(content))
        {
            var slug = Normalize(m.Groups[1].Value);
            if (slug.Length > 0)
                set.Add(slug);
        }

        foreach (Match m in WikiPathLink.Matches(content))
        {
            var slug = Normalize(m.Groups[1].Value);
            if (slug.Length > 0)
                set.Add(slug);
        }

        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string Normalize(string raw) =>
        raw.Trim().Trim('/').ToLowerInvariant();
}

using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Wiki.DBLayer;
using SailNora.Product.Wiki.Dtos;

namespace SailNora.Product.Wiki.Service;

[Scoped]
public sealed class WikiGraphService
{
    private readonly MooRepository<KnWikiPage> _pages;

    public WikiGraphService(MooRepository<KnWikiPage> pages) => _pages = pages;

    public WikiGraphDataDto Build(Guid spaceId, string? knowledgeBaseId = null, int limit = 500)
    {
        limit = Math.Clamp(limit <= 0 ? 500 : limit, 1, 2000);

        IEnumerable<KnWikiPage> query = _pages.Repo.GetList(p => p.SpaceId == spaceId);
        if (!string.IsNullOrWhiteSpace(knowledgeBaseId))
            query = query.Where(p => p.KnowledgeBaseId == knowledgeBaseId);

        var pages = query
            .Where(p => !string.Equals(p.Slug, "_index", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Title)
            .ToList();

        var slugSet = pages.Select(p => p.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var edges = new List<WikiGraphEdgeDto>();
        var outCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in pages)
        {
            var links = WikiLinkExtractor.ExtractOutLinks(page.Content);
            var count = 0;
            foreach (var target in links)
            {
                if (!slugSet.Contains(target))
                    continue;
                if (string.Equals(target, page.Slug, StringComparison.OrdinalIgnoreCase))
                    continue;
                edges.Add(new WikiGraphEdgeDto { Source = page.Slug, Target = target });
                count++;
            }

            outCounts[page.Slug] = count;
        }

        var truncated = pages.Count > limit;
        var nodes = pages
            .Take(limit)
            .Select(p => new WikiGraphNodeDto
            {
                Slug = p.Slug,
                Title = p.Title,
                LinkCount = outCounts.GetValueOrDefault(p.Slug)
            })
            .ToList();

        var nodeSlugs = nodes.Select(n => n.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var edgeSlice = edges
            .Where(e => nodeSlugs.Contains(e.Source) && nodeSlugs.Contains(e.Target))
            .Take(limit * 4)
            .ToList();

        return new WikiGraphDataDto
        {
            Nodes = nodes,
            Edges = edgeSlice,
            Meta = new WikiGraphMetaDto
            {
                Mode = "overview",
                Total = pages.Count,
                Returned = nodes.Count,
                Truncated = truncated || edges.Count > edgeSlice.Count
            }
        };
    }
}

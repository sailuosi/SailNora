using System.Text;
using Microsoft.Extensions.Logging;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Wiki.DBLayer;
using SailNora.Product.Wiki.Dtos;

namespace SailNora.Product.Wiki.Jobs;

/// <summary>对齐上游 asynq <c>wiki:finalize</c>：标记页面 finalized 并重建 <c>_index</c> 摘要（无 LLM）。</summary>
[Scoped]
public sealed class WikiFinalizeJob
{
    private readonly MooRepository<KnWikiPage> _pages;
    private readonly ILogger<WikiFinalizeJob> _logger;

    public WikiFinalizeJob(MooRepository<KnWikiPage> pages, ILogger<WikiFinalizeJob> logger)
    {
        _pages = pages;
        _logger = logger;
    }

    public Task<WikiFinalizeResultDto> ExecuteAsync(
        WikiFinalizePayload payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<KnWikiPage> query = _pages.Repo.GetList(p => p.SpaceId == payload.SpaceId);
        if (!string.IsNullOrWhiteSpace(payload.KnowledgeBaseId))
            query = query.Where(p => p.KnowledgeBaseId == payload.KnowledgeBaseId);

        var pages = query
            .Where(p => !string.Equals(p.Slug, "_index", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Title)
            .ToList();

        var now = DateTime.Now;
        var count = 0;
        foreach (var p in pages)
        {
            if (!string.Equals(p.Status, WikiPageStatuses.Finalized, StringComparison.OrdinalIgnoreCase))
            {
                p.Status = WikiPageStatuses.Finalized;
                p.UpdateTime = now;
                _pages.Repo.Update(p);
                count++;
            }
        }

        var indexBody = new StringBuilder();
        indexBody.AppendLine("# Wiki Index");
        indexBody.AppendLine();
        indexBody.AppendLine($"Generated: {now:yyyy-MM-dd HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(payload.KnowledgeBaseId))
            indexBody.AppendLine($"KnowledgeBase: `{payload.KnowledgeBaseId}`");
        indexBody.AppendLine();
        indexBody.AppendLine("## Pages");
        foreach (var p in pages)
            indexBody.AppendLine($"- [{p.Title}](/wiki/{p.Slug}) `{p.Status}`");

        KnWikiPage? index;
        if (!string.IsNullOrWhiteSpace(payload.KnowledgeBaseId))
        {
            index = _pages.Repo.GetFirst(p =>
                p.SpaceId == payload.SpaceId
                && p.Slug == "_index"
                && p.KnowledgeBaseId == payload.KnowledgeBaseId);
        }
        else
        {
            index = _pages.Repo.GetFirst(p =>
                p.SpaceId == payload.SpaceId && p.Slug == "_index");
        }

        if (index is null)
        {
            index = new KnWikiPage
            {
                Id = Guid.NewGuid().ToString(),
                SpaceId = payload.SpaceId,
                KnowledgeBaseId = payload.KnowledgeBaseId,
                Slug = "_index",
                Title = "Wiki Index",
                Content = indexBody.ToString().Trim(),
                Status = WikiPageStatuses.Finalized,
                CreateTime = now,
                UpdateTime = now
            };
            _pages.Repo.Insert(index);
        }
        else
        {
            index.Title = "Wiki Index";
            index.Content = indexBody.ToString().Trim();
            index.Status = WikiPageStatuses.Finalized;
            index.KnowledgeBaseId = payload.KnowledgeBaseId ?? index.KnowledgeBaseId;
            index.UpdateTime = now;
            _pages.Repo.Update(index);
        }

        _logger.LogInformation(
            "wiki:finalize space={SpaceId} kb={Kb} finalized={Count} index={IndexId}",
            payload.SpaceId, payload.KnowledgeBaseId, count, index.Id);

        return Task.FromResult(new WikiFinalizeResultDto
        {
            FinalizedCount = count,
            IndexPageId = index.Id,
            IndexSlug = index.Slug
        });
    }
}

public interface IWikiFinalizeEnqueue
{
    void Enqueue(WikiFinalizePayload payload);
}

[Scoped]
public sealed class InlineWikiFinalizeEnqueue : IWikiFinalizeEnqueue
{
    private readonly WikiFinalizeJob _job;

    public InlineWikiFinalizeEnqueue(WikiFinalizeJob job) => _job = job;

    public void Enqueue(WikiFinalizePayload payload) =>
        _job.ExecuteAsync(payload).GetAwaiter().GetResult();
}

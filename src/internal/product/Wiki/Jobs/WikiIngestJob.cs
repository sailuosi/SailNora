using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Wiki.DBLayer;
using SailNora.Product.Wiki.Dtos;

namespace SailNora.Product.Wiki.Jobs;

/// <summary>对齐上游 asynq <c>wiki:ingest</c>：从已完成文档生成/更新 Wiki 页。</summary>
public sealed class WikiIngestJob : IScoped
{
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly MooRepository<KnChunk> _chunks;
    private readonly MooRepository<KnWikiPage> _pages;
    private readonly ILogger<WikiIngestJob> _logger;

    public WikiIngestJob(
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks,
        MooRepository<KnWikiPage> pages,
        ILogger<WikiIngestJob> logger)
    {
        _knowledges = knowledges;
        _chunks = chunks;
        _pages = pages;
        _logger = logger;
    }

    public Task<WikiIngestResultDto?> ExecuteAsync(
        WikiIngestPayload payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var knowledge = _knowledges.Repo.GetById(payload.KnowledgeId);
        if (knowledge is null)
        {
            _logger.LogWarning("wiki:ingest knowledge missing {KnowledgeId}", payload.KnowledgeId);
            return Task.FromResult<WikiIngestResultDto?>(null);
        }

        if (knowledge.SpaceId != payload.SpaceId)
            throw new UnauthorizedAccessException("无权访问");

        if (!string.Equals(knowledge.ParseStatus, ParseStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("文档尚未处理完成，无法整理 Wiki");

        var chunks = _chunks.Repo.GetList(c => c.KnowledgeId == knowledge.Id)
            .OrderBy(c => c.ChunkIndex)
            .ToList();
        var body = new StringBuilder();
        body.Append("# ").Append(knowledge.Title).AppendLine().AppendLine();
        foreach (var c in chunks.Take(20))
            body.AppendLine(c.Content).AppendLine();

        var now = DateTime.Now;
        var existing = _pages.Repo.GetFirst(p =>
            p.SpaceId == payload.SpaceId && p.SourceKnowledgeId == knowledge.Id);
        if (existing is not null)
        {
            existing.Title = knowledge.Title;
            existing.Content = body.ToString().Trim();
            existing.KnowledgeBaseId = knowledge.KnowledgeBaseId;
            existing.UpdateTime = now;
            _pages.Repo.Update(existing);
            _logger.LogInformation("wiki:ingest updated page {PageId} from {KnowledgeId}", existing.Id, knowledge.Id);
            return Task.FromResult<WikiIngestResultDto?>(new WikiIngestResultDto
            {
                KnowledgeId = knowledge.Id,
                WikiPageId = existing.Id,
                Slug = existing.Slug,
                Title = existing.Title
            });
        }

        var slug = Slugify(knowledge.Title);
        if (_pages.Repo.GetFirst(p => p.SpaceId == payload.SpaceId && p.Slug == slug) is not null)
            slug = $"{slug}-{knowledge.Id[..8]}";

        var page = new KnWikiPage
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = payload.SpaceId,
            KnowledgeBaseId = knowledge.KnowledgeBaseId,
            SourceKnowledgeId = knowledge.Id,
            Slug = slug,
            Title = knowledge.Title,
            Content = body.ToString().Trim(),
            CreateTime = now,
            UpdateTime = now
        };
        _pages.Repo.Insert(page);
        _logger.LogInformation("wiki:ingest created page {PageId} from {KnowledgeId}", page.Id, knowledge.Id);
        return Task.FromResult<WikiIngestResultDto?>(new WikiIngestResultDto
        {
            KnowledgeId = knowledge.Id,
            WikiPageId = page.Id,
            Slug = page.Slug,
            Title = page.Title
        });
    }

    private static string Slugify(string raw)
    {
        var s = raw.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "-");
        s = Regex.Replace(s, @"[^a-z0-9\u4e00-\u9fff\-]+", "");
        return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("N")[..8] : s;
    }
}

public interface IWikiIngestEnqueue
{
    void Enqueue(WikiIngestPayload payload);
}

public sealed class InlineWikiIngestEnqueue : IWikiIngestEnqueue, IScoped
{
    private readonly WikiIngestJob _job;

    public InlineWikiIngestEnqueue(WikiIngestJob job) => _job = job;

    public void Enqueue(WikiIngestPayload payload) =>
        _job.ExecuteAsync(payload).GetAwaiter().GetResult();
}

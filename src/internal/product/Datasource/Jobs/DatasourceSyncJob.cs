using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Storage;
using SailNora.Product.Datasource.DBLayer;
using SailNora.Product.Datasource.Dtos;
using SailNora.Product.Datasource.Service;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.Product.Datasource.Jobs;

/// <summary>对齐上游 asynq <c>datasource:sync</c>（F6：rss + gitlab）。</summary>
public sealed class DatasourceSyncJob : IScoped
{
    private readonly MooRepository<KnDatasource> _sources;
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly ILocalFileStorage _storage;
    private readonly IDocumentProcessEnqueue _enqueue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatasourceSyncJob> _logger;

    public DatasourceSyncJob(
        MooRepository<KnDatasource> sources,
        MooRepository<KnKnowledgeBase> bases,
        MooRepository<KnKnowledge> knowledges,
        ILocalFileStorage storage,
        IDocumentProcessEnqueue enqueue,
        IHttpClientFactory httpClientFactory,
        ILogger<DatasourceSyncJob> logger)
    {
        _sources = sources;
        _bases = bases;
        _knowledges = knowledges;
        _storage = storage;
        _enqueue = enqueue;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<DatasourceSyncResultDto> ExecuteAsync(
        DatasourceSyncPayload payload,
        CancellationToken cancellationToken = default)
    {
        var ds = _sources.Repo.GetById(payload.DatasourceId);
        if (ds is null || ds.SpaceId != payload.SpaceId)
        {
            _logger.LogWarning("datasource:sync missing {Id}", payload.DatasourceId);
            return new DatasourceSyncResultDto
            {
                DatasourceId = payload.DatasourceId,
                SyncStatus = DatasourceSyncStatuses.Failed,
                Error = "数据源不存在"
            };
        }

        var kb = _bases.Repo.GetById(ds.KnowledgeBaseId);
        if (kb is null || kb.SpaceId != ds.SpaceId)
            throw new InvalidOperationException("绑定知识库无效");

        ds.SyncStatus = DatasourceSyncStatuses.Syncing;
        ds.LastError = null;
        _sources.Repo.Update(ds);

        try
        {
            var (imported, skipped) = ds.Type.ToLowerInvariant() switch
            {
                DatasourceTypes.Rss => await SyncRssAsync(ds, cancellationToken),
                DatasourceTypes.Gitlab => await SyncGitlabAsync(ds, cancellationToken),
                _ => throw new InvalidOperationException($"暂不支持数据源类型: {ds.Type}")
            };

            ds.SyncStatus = DatasourceSyncStatuses.Completed;
            ds.LastSyncAt = DateTime.Now;
            ds.ImportedCount += imported;
            ds.LastError = null;
            _sources.Repo.Update(ds);

            _logger.LogInformation(
                "datasource:sync completed {Id} type={Type} imported={Imported} skipped={Skipped}",
                ds.Id, ds.Type, imported, skipped);

            return new DatasourceSyncResultDto
            {
                DatasourceId = ds.Id,
                SyncStatus = ds.SyncStatus,
                Imported = imported,
                Skipped = skipped
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "datasource:sync failed {Id}", ds.Id);
            ds.SyncStatus = DatasourceSyncStatuses.Failed;
            ds.LastError = ex.Message;
            _sources.Repo.Update(ds);
            return new DatasourceSyncResultDto
            {
                DatasourceId = ds.Id,
                SyncStatus = DatasourceSyncStatuses.Failed,
                Error = ex.Message
            };
        }
    }

    private async Task<(int Imported, int Skipped)> SyncRssAsync(KnDatasource ds, CancellationToken cancellationToken)
    {
        var xml = await FetchTextAsync(ds.Url, cancellationToken);
        var items = RssFeedParser.Parse(xml);
        var imported = 0;
        var skipped = 0;

        foreach (var item in items.Take(50))
        {
            var body = $"# {item.Title}\n\nSource: {item.Link}\n\n{item.Content}\n";
            var (ok, wasSkip) = await ImportMarkdownAsync(
                ds, item.Guid, item.Title, body, knowledgeType: "rss", cancellationToken);
            if (wasSkip) skipped++;
            else if (ok) imported++;
        }

        return (imported, skipped);
    }

    private async Task<(int Imported, int Skipped)> SyncGitlabAsync(KnDatasource ds, CancellationToken cancellationToken)
    {
        var json = await FetchTextAsync(ds.Url, cancellationToken);
        var items = GitLabTreeParser.Parse(json);
        var imported = 0;
        var skipped = 0;

        foreach (var item in items.Take(50))
        {
            var title = Path.GetFileNameWithoutExtension(item.Path);
            if (string.IsNullOrWhiteSpace(title))
                title = item.Path;
            var content = string.IsNullOrWhiteSpace(item.Content)
                ? $"(gitlab blob) `{item.Path}`"
                : item.Content!;
            var body = $"# {title}\n\nPath: `{item.Path}`\n\n{content}\n";
            var (ok, wasSkip) = await ImportMarkdownAsync(
                ds, item.Guid, title, body, knowledgeType: "gitlab", cancellationToken);
            if (wasSkip) skipped++;
            else if (ok) imported++;
        }

        return (imported, skipped);
    }

    private async Task<(bool Ok, bool Skipped)> ImportMarkdownAsync(
        KnDatasource ds,
        string guid,
        string title,
        string body,
        string knowledgeType,
        CancellationToken cancellationToken)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(ds.Id + "|" + guid))).ToLowerInvariant();
        if (_knowledges.Repo.GetFirst(k =>
                k.KnowledgeBaseId == ds.KnowledgeBaseId && k.FileHash == hash) is not null)
            return (false, true);

        var knowledgeId = Guid.NewGuid().ToString();
        var fileName = Sanitize($"{title}.md");
        var relative = $"{ds.SpaceId:N}/{knowledgeId}/{fileName}";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        await _storage.SaveAsync(stream, relative, cancellationToken);

        _knowledges.Repo.Insert(new KnKnowledge
        {
            Id = knowledgeId,
            SpaceId = ds.SpaceId,
            KnowledgeBaseId = ds.KnowledgeBaseId,
            Type = knowledgeType,
            Title = title,
            FileName = fileName,
            FileType = "text/markdown",
            FileSize = stream.Length,
            FilePath = relative,
            FileHash = hash,
            ParseStatus = ParseStatuses.Pending,
            CreateTime = DateTime.Now
        });

        _enqueue.Enqueue(new DocumentProcessPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = ds.SpaceId,
            KnowledgeId = knowledgeId,
            KnowledgeBaseId = ds.KnowledgeBaseId,
            FilePath = relative,
            FileName = fileName,
            FileType = "text/markdown"
        });
        return (true, false);
    }

    private async Task<string> FetchTextAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("datasource");
        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "item.md" : name;
    }
}

public interface IDatasourceSyncEnqueue
{
    void Enqueue(DatasourceSyncPayload payload);
}

public sealed class InlineDatasourceSyncEnqueue : IDatasourceSyncEnqueue, IScoped
{
    private readonly DatasourceSyncJob _job;

    public InlineDatasourceSyncEnqueue(DatasourceSyncJob job) => _job = job;

    public void Enqueue(DatasourceSyncPayload payload) =>
        _job.ExecuteAsync(payload).GetAwaiter().GetResult();
}

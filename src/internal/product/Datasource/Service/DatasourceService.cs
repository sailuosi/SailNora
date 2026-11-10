using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Datasource.DBLayer;
using SailNora.Product.Datasource.Dtos;
using SailNora.Product.Datasource.Jobs;
using SailNora.Product.Knowledge.DBLayer;

namespace SailNora.Product.Datasource.Service;

public sealed class DatasourceService : IScoped
{
    private readonly MooRepository<KnDatasource> _sources;
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly DatasourceSyncJob _syncJob;
    private readonly IDatasourceSyncEnqueue _enqueue;

    public DatasourceService(
        MooRepository<KnDatasource> sources,
        MooRepository<KnKnowledgeBase> bases,
        DatasourceSyncJob syncJob,
        IDatasourceSyncEnqueue enqueue)
    {
        _sources = sources;
        _bases = bases;
        _syncJob = syncJob;
        _enqueue = enqueue;
    }

    public DatasourceDto Create(Guid spaceId, CreateDatasourceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");
        if (string.IsNullOrWhiteSpace(input.Url))
            throw new InvalidOperationException("URL 不能为空");
        if (string.IsNullOrWhiteSpace(input.KnowledgeBaseId))
            throw new InvalidOperationException("知识库不能为空");

        var kb = _bases.Repo.GetById(input.KnowledgeBaseId)
                 ?? throw new InvalidOperationException("知识库不存在");
        if (kb.SpaceId != spaceId)
            throw new UnauthorizedAccessException("知识库不属于当前空间");

        var type = string.IsNullOrWhiteSpace(input.Type) ? DatasourceTypes.Rss : input.Type.Trim();
        if (!string.Equals(type, DatasourceTypes.Rss, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("F6 仅支持 type=rss");

        var entity = new KnDatasource
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            KnowledgeBaseId = kb.Id,
            Name = input.Name.Trim(),
            Type = DatasourceTypes.Rss,
            Url = input.Url.Trim(),
            Enabled = true,
            SyncStatus = DatasourceSyncStatuses.Idle,
            CreateTime = DateTime.Now
        };
        _sources.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<DatasourceDto> List(Guid spaceId) =>
        _sources.Repo.GetList(s => s.SpaceId == spaceId)
            .OrderByDescending(s => s.CreateTime)
            .Select(ToDto)
            .ToList();

    public DatasourceDto? Get(Guid spaceId, string id)
    {
        var s = _sources.Repo.GetById(id);
        if (s is null || s.SpaceId != spaceId)
            return null;
        return ToDto(s);
    }

    public void Delete(Guid spaceId, string id)
    {
        var s = _sources.Repo.GetById(id)
                ?? throw new InvalidOperationException("数据源不存在");
        if (s.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        _sources.Repo.Delete(s);
    }

    /// <summary>同步执行并返回结果（冒烟友好）；同时保留入队接口供 Hangfire。</summary>
    public async Task<DatasourceSyncResultDto> SyncNowAsync(
        Guid spaceId,
        string id,
        CancellationToken cancellationToken = default)
    {
        var s = _sources.Repo.GetById(id)
                ?? throw new InvalidOperationException("数据源不存在");
        if (s.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        return await _syncJob.ExecuteAsync(new DatasourceSyncPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId,
            DatasourceId = id
        }, cancellationToken);
    }

    public void EnqueueSync(Guid spaceId, string id)
    {
        var s = _sources.Repo.GetById(id)
                ?? throw new InvalidOperationException("数据源不存在");
        if (s.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        _enqueue.Enqueue(new DatasourceSyncPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId,
            DatasourceId = id
        });
    }

    private static DatasourceDto ToDto(KnDatasource s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Type = s.Type,
        Url = s.Url,
        KnowledgeBaseId = s.KnowledgeBaseId,
        Enabled = s.Enabled,
        SyncStatus = s.SyncStatus,
        LastSyncAt = s.LastSyncAt,
        LastError = s.LastError,
        ImportedCount = s.ImportedCount,
        CreateTime = s.CreateTime
    };
}

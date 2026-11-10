using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Memory.DBLayer;
using SailNora.Product.Memory.Dtos;
using SailNora.Product.Memory.Jobs;

namespace SailNora.Product.Memory.Service;

[Scoped]
public sealed class MemoryService
{
    private readonly MooRepository<KnMemory> _memories;
    private readonly MemoryExtractJob _job;
    private readonly IMemoryExtractEnqueue _enqueue;

    public MemoryService(
        MooRepository<KnMemory> memories,
        MemoryExtractJob job,
        IMemoryExtractEnqueue enqueue)
    {
        _memories = memories;
        _job = job;
        _enqueue = enqueue;
    }

    public List<MemoryDto> List(Guid spaceId, Guid userId) =>
        _memories.Repo.GetList(m => m.SpaceId == spaceId && m.UserId == userId)
            .OrderByDescending(m => m.CreateTime)
            .Select(ToDto)
            .ToList();

    public void Delete(Guid spaceId, Guid userId, string id)
    {
        var m = _memories.Repo.GetById(id)
                ?? throw new InvalidOperationException("记忆不存在");
        if (m.SpaceId != spaceId || m.UserId != userId)
            throw new UnauthorizedAccessException("无权访问");
        _memories.Repo.Delete(m);
    }

    public Task<MemoryExtractResultDto> ExtractNowAsync(
        Guid spaceId,
        Guid userId,
        string sessionId,
        CancellationToken cancellationToken = default) =>
        _job.ExecuteAsync(new MemoryExtractPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId,
            UserId = userId,
            SessionId = sessionId
        }, cancellationToken);

    public void EnqueueExtract(Guid spaceId, Guid userId, string sessionId) =>
        _enqueue.Enqueue(new MemoryExtractPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId,
            UserId = userId,
            SessionId = sessionId
        });

    private static MemoryDto ToDto(KnMemory m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        Content = m.Content,
        Source = m.Source,
        CreateTime = m.CreateTime
    };
}

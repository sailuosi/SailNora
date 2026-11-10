using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.DBLayer;
using SailNora.Product.Chat.Service;
using SailNora.Product.Memory.DBLayer;
using SailNora.Product.Memory.Dtos;

namespace SailNora.Product.Memory.Jobs;

/// <summary>对齐上游 asynq <c>memory:extract</c>（F6 启发式抽取，非 LLM 蒸馏）。</summary>
public sealed class MemoryExtractJob : IScoped
{
    private readonly SessionService _sessions;
    private readonly MooRepository<KnMemory> _memories;
    private readonly ILogger<MemoryExtractJob> _logger;

    public MemoryExtractJob(
        SessionService sessions,
        MooRepository<KnMemory> memories,
        ILogger<MemoryExtractJob> logger)
    {
        _sessions = sessions;
        _memories = memories;
        _logger = logger;
    }

    public Task<MemoryExtractResultDto> ExecuteAsync(
        MemoryExtractPayload payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = _sessions.GetOwned(payload.SpaceId, payload.UserId, payload.SessionId)
                      ?? throw new InvalidOperationException("会话不存在");

        var messages = _sessions.ListMessages(session.Id);
        var extracted = 0;
        var skipped = 0;

        foreach (var msg in messages.Where(m => m.Role == "user"))
        {
            var content = Normalize(msg.Content);
            if (content.Length < 8)
            {
                skipped++;
                continue;
            }

            // 过长截断为一条「用户偏好/事实」候选
            if (content.Length > 240)
                content = content[..240].Trim();

            var hash = Hash(payload.UserId, content);
            if (_memories.Repo.GetFirst(m =>
                    m.SpaceId == payload.SpaceId && m.UserId == payload.UserId && m.ContentHash == hash) is not null)
            {
                skipped++;
                continue;
            }

            _memories.Repo.Insert(new KnMemory
            {
                Id = Guid.NewGuid().ToString(),
                SpaceId = payload.SpaceId,
                UserId = payload.UserId,
                SessionId = session.Id,
                Content = content,
                ContentHash = hash,
                Source = "session",
                CreateTime = DateTime.Now
            });
            extracted++;
        }

        _logger.LogInformation(
            "memory:extract session={SessionId} extracted={Extracted} skipped={Skipped}",
            session.Id, extracted, skipped);

        return Task.FromResult(new MemoryExtractResultDto
        {
            SessionId = session.Id,
            Extracted = extracted,
            Skipped = skipped
        });
    }

    private static string Normalize(string raw) =>
        string.Join(' ', raw.Replace('\r', ' ').Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string Hash(Guid userId, string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString("N") + "|" + content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public interface IMemoryExtractEnqueue
{
    void Enqueue(MemoryExtractPayload payload);
}

public sealed class InlineMemoryExtractEnqueue : IMemoryExtractEnqueue, IScoped
{
    private readonly MemoryExtractJob _job;

    public InlineMemoryExtractEnqueue(MemoryExtractJob job) => _job = job;

    public void Enqueue(MemoryExtractPayload payload) =>
        _job.ExecuteAsync(payload).GetAwaiter().GetResult();
}

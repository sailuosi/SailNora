using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.Service;
using SailNora.Product.Memory.DBLayer;
using SailNora.Product.Memory.Dtos;
using SailNora.Product.Memory.Service;

namespace SailNora.Product.Memory.Jobs;

/// <summary>对齐上游 asynq <c>memory:extract</c>（heuristic 或 llm 蒸馏）。</summary>
public sealed class MemoryExtractJob : IScoped
{
    private readonly SessionService _sessions;
    private readonly MooRepository<KnMemory> _memories;
    private readonly MemoryDistiller _distiller;
    private readonly ILogger<MemoryExtractJob> _logger;

    public MemoryExtractJob(
        SessionService sessions,
        MooRepository<KnMemory> memories,
        MemoryDistiller distiller,
        ILogger<MemoryExtractJob> logger)
    {
        _sessions = sessions;
        _memories = memories;
        _distiller = distiller;
        _logger = logger;
    }

    public async Task<MemoryExtractResultDto> ExecuteAsync(
        MemoryExtractPayload payload,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var session = _sessions.GetOwned(payload.SpaceId, payload.UserId, payload.SessionId)
                      ?? throw new InvalidOperationException("会话不存在");

        var messages = _sessions.ListMessages(session.Id);
        var (facts, modeUsed) = await _distiller.DistillAsync(messages, cancellationToken);
        var extracted = 0;
        var skipped = 0;
        var source = string.Equals(modeUsed, "llm", StringComparison.OrdinalIgnoreCase)
            ? "session-llm"
            : "session";

        foreach (var fact in facts)
        {
            var content = fact.Trim();
            if (content.Length < 4)
            {
                skipped++;
                continue;
            }

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
                Source = source,
                CreateTime = DateTime.Now
            });
            extracted++;
        }

        _logger.LogInformation(
            "memory:extract session={SessionId} mode={Mode} extracted={Extracted} skipped={Skipped}",
            session.Id, modeUsed, extracted, skipped);

        return new MemoryExtractResultDto
        {
            SessionId = session.Id,
            Extracted = extracted,
            Skipped = skipped,
            Mode = modeUsed
        };
    }

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

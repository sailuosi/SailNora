using System.Text.Json;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.DBLayer;
using SailNora.Product.Mcp.Dtos;

namespace SailNora.Product.Mcp.Service;

public sealed class HitlService : IScoped
{
    private readonly MooRepository<KnHitlPending> _pendings;

    public HitlService(MooRepository<KnHitlPending> pendings) => _pendings = pendings;

    public HitlPendingDto CreatePending(Guid spaceId, string sessionId, string toolName, object? arguments)
    {
        var entity = new KnHitlPending
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            SessionId = sessionId,
            ToolName = toolName,
            ArgumentsJson = arguments is null
                ? null
                : JsonSerializer.Serialize(arguments, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
            Status = HitlStatuses.Pending,
            CreateTime = DateTime.Now
        };
        _pendings.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<HitlPendingDto> List(Guid spaceId, string sessionId, string? status = null)
    {
        var rows = _pendings.Repo.GetList(p => p.SpaceId == spaceId && p.SessionId == sessionId);
        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(p => p.Status == status).ToList();
        return rows.OrderByDescending(p => p.CreateTime).Select(ToDto).ToList();
    }

    public HitlPendingDto Resolve(Guid spaceId, string sessionId, string pendingId, ResolveHitlInput input)
    {
        var p = _pendings.Repo.GetById(pendingId)
                ?? throw new InvalidOperationException("审批单不存在");
        if (p.SpaceId != spaceId || p.SessionId != sessionId)
            throw new UnauthorizedAccessException("无权访问");
        if (p.Status != HitlStatuses.Pending)
            throw new InvalidOperationException("审批单已处理");

        var decision = (input.Decision ?? "approve").Trim().ToLowerInvariant();
        p.Status = decision is "reject" or "rejected" ? HitlStatuses.Rejected : HitlStatuses.Approved;
        p.Reason = input.Reason?.Trim();
        p.ResolvedAt = DateTime.Now;
        _pendings.Repo.Update(p);
        return ToDto(p);
    }

    public async Task<string> WaitForDecisionAsync(
        string pendingId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (timeout <= TimeSpan.Zero)
        {
            AutoApprove(pendingId, "timeout=0 auto-approve");
            return HitlStatuses.Approved;
        }

        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var p = _pendings.Repo.GetById(pendingId);
            if (p is null)
                return HitlStatuses.Rejected;
            if (p.Status != HitlStatuses.Pending)
                return p.Status;
            await Task.Delay(200, cancellationToken);
        }

        AutoApprove(pendingId, "wait timeout auto-approve");
        return HitlStatuses.Approved;
    }

    private void AutoApprove(string pendingId, string reason)
    {
        var p = _pendings.Repo.GetById(pendingId);
        if (p is null || p.Status != HitlStatuses.Pending)
            return;
        p.Status = HitlStatuses.Approved;
        p.Reason = reason;
        p.ResolvedAt = DateTime.Now;
        _pendings.Repo.Update(p);
    }

    private static HitlPendingDto ToDto(KnHitlPending p) => new()
    {
        Id = p.Id,
        SessionId = p.SessionId,
        ToolName = p.ToolName,
        ArgumentsJson = p.ArgumentsJson,
        Status = p.Status,
        Reason = p.Reason,
        CreateTime = p.CreateTime,
        ResolvedAt = p.ResolvedAt
    };
}

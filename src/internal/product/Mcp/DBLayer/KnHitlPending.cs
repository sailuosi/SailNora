using mooSQL.data;

namespace SailNora.Product.Mcp.DBLayer;

[SooTable("KnHitlPending", Caption = "HITL 工具审批")]
public class KnHitlPending
{
    [SooColumn("Kh_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kh_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kh_SessionId", Length = 36, CanBeNull = false)]
    public string SessionId { get; set; } = string.Empty;

    [SooColumn("Kh_ToolName", Length = 128, CanBeNull = false)]
    public string ToolName { get; set; } = string.Empty;

    [SooColumn("Kh_ArgumentsJson", Length = -1, CanBeNull = true)]
    public string? ArgumentsJson { get; set; }

    /// <summary>pending | approved | rejected</summary>
    [SooColumn("Kh_Status", Length = 32, CanBeNull = false)]
    public string Status { get; set; } = HitlStatuses.Pending;

    [SooColumn("Kh_Reason", Length = 512, CanBeNull = true)]
    public string? Reason { get; set; }

    [SooColumn("Kh_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Kh_ResolvedAt", CanBeNull = true)]
    public DateTime? ResolvedAt { get; set; }
}

public static class HitlStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

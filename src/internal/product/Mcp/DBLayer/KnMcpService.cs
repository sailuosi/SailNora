using mooSQL.data;

namespace SailNora.Product.Mcp.DBLayer;

[SooTable("KnMcpService", Caption = "出站 MCP 服务")]
public class KnMcpService
{
    [SooColumn("Kms_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kms_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kms_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Kms_Transport", Length = 32, CanBeNull = false)]
    public string Transport { get; set; } = "sse";

    [SooColumn("Kms_Url", Length = 1024, CanBeNull = false)]
    public string Url { get; set; } = string.Empty;

    [SooColumn("Kms_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Kms_RequireApproval", CanBeNull = false)]
    public bool RequireApproval { get; set; }

    [SooColumn("Kms_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

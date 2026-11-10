using mooSQL.data;

namespace SailNora.Product.Mcp.DBLayer;

[SooTable("KnMcpEndpoint", Caption = "入站 MCP Endpoint")]
public class KnMcpEndpoint
{
    [SooColumn("Kme_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kme_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kme_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Kme_TokenHash", Length = 128, CanBeNull = false)]
    public string TokenHash { get; set; } = string.Empty;

    [SooColumn("Kme_TokenPrefix", Length = 16, CanBeNull = false)]
    public string TokenPrefix { get; set; } = string.Empty;

    [SooColumn("Kme_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Kme_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

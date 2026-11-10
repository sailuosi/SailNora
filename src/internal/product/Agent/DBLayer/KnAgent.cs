using mooSQL.data;

namespace SailNora.Product.Agent.DBLayer;

[SooTable("KnAgent", Caption = "自定义 Agent")]
public class KnAgent
{
    [SooColumn("Ka_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Ka_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Ka_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Ka_Instructions", Length = -1, CanBeNull = true)]
    public string? Instructions { get; set; }

    [SooColumn("Ka_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Ka_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

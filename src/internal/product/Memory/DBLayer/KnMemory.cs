using mooSQL.data;

namespace SailNora.Product.Memory.DBLayer;

[SooTable("KnMemory", Caption = "个人记忆")]
public class KnMemory
{
    [SooColumn("Km_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Km_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Km_UserId", CanBeNull = false)]
    public Guid UserId { get; set; }

    [SooColumn("Km_SessionId", Length = 36, CanBeNull = true)]
    public string? SessionId { get; set; }

    [SooColumn("Km_Content", Length = -1, CanBeNull = false)]
    public string Content { get; set; } = string.Empty;

    [SooColumn("Km_ContentHash", Length = 128, CanBeNull = false)]
    public string ContentHash { get; set; } = string.Empty;

    [SooColumn("Km_Source", Length = 64, CanBeNull = false)]
    public string Source { get; set; } = "session";

    [SooColumn("Km_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

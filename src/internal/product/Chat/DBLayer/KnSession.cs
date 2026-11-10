using mooSQL.data;

namespace SailNora.Product.Chat.DBLayer;

[SooTable("KnSession", Caption = "会话")]
public class KnSession
{
    [SooColumn("Ks_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Ks_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Ks_UserId", CanBeNull = false)]
    public Guid UserId { get; set; }

    [SooColumn("Ks_Title", Length = 256, CanBeNull = false)]
    public string Title { get; set; } = "新会话";

    [SooColumn("Ks_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Ks_UpdateTime", CanBeNull = false)]
    public DateTime UpdateTime { get; set; }
}

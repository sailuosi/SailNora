using mooSQL.data;

namespace SailNora.Product.Chat.DBLayer;

[SooTable("KnMessage", Caption = "会话消息")]
public class KnMessage
{
    [SooColumn("Km_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Km_SessionId", Length = 36, CanBeNull = false)]
    public string SessionId { get; set; } = string.Empty;

    [SooColumn("Km_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    /// <summary>user | assistant | system</summary>
    [SooColumn("Km_Role", Length = 32, CanBeNull = false)]
    public string Role { get; set; } = "user";

    [SooColumn("Km_Content", Length = -1, CanBeNull = false)]
    public string Content { get; set; } = string.Empty;

    [SooColumn("Km_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

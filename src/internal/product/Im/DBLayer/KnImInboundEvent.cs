using mooSQL.data;

namespace SailNora.Product.Im.DBLayer;

[SooTable("KnImInboundEvent", Caption = "IM 入站事件")]
public class KnImInboundEvent
{
    [SooColumn("Kie_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kie_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kie_ChannelId", Length = 36, CanBeNull = false)]
    public string ChannelId { get; set; } = string.Empty;

    [SooColumn("Kie_ExternalUserId", Length = 128, CanBeNull = true)]
    public string? ExternalUserId { get; set; }

    [SooColumn("Kie_Text", Length = -1, CanBeNull = false)]
    public string Text { get; set; } = string.Empty;

    [SooColumn("Kie_SessionId", Length = 36, CanBeNull = true)]
    public string? SessionId { get; set; }

    [SooColumn("Kie_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

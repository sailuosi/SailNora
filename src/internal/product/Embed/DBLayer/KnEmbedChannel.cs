using mooSQL.data;

namespace SailNora.Product.Embed.DBLayer;

[SooTable("KnEmbedChannel", Caption = "Embed 渠道")]
public class KnEmbedChannel
{
    [SooColumn("Kec_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kec_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kec_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Kec_TokenHash", Length = 128, CanBeNull = false)]
    public string TokenHash { get; set; } = string.Empty;

    [SooColumn("Kec_TokenPrefix", Length = 16, CanBeNull = false)]
    public string TokenPrefix { get; set; } = string.Empty;

    /// <summary>JSON 数组：绑定知识库 ID。</summary>
    [SooColumn("Kec_KnowledgeBaseIdsJson", Length = -1, CanBeNull = true)]
    public string? KnowledgeBaseIdsJson { get; set; }

    [SooColumn("Kec_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Kec_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

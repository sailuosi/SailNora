using mooSQL.data;

namespace SailNora.Product.Im.DBLayer;

[SooTable("KnImChannel", Caption = "IM 渠道")]
public class KnImChannel
{
    [SooColumn("Kim_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kim_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kim_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>generic | feishu | wecom | …</summary>
    [SooColumn("Kim_Provider", Length = 32, CanBeNull = false)]
    public string Provider { get; set; } = "generic";

    /// <summary>Webhook HMAC 密钥（明文存储，F6 最小）。</summary>
    [SooColumn("Kim_Secret", Length = 256, CanBeNull = false)]
    public string Secret { get; set; } = string.Empty;

    [SooColumn("Kim_KnowledgeBaseId", Length = 36, CanBeNull = true)]
    public string? KnowledgeBaseId { get; set; }

    [SooColumn("Kim_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Kim_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

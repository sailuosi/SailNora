using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

[SooTable("KnKnowledgeTag", Caption = "知识标签")]
public class KnKnowledgeTag
{
    [SooColumn("Kkt_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kkt_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kkt_KnowledgeBaseId", Length = 36, CanBeNull = false)]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    [SooColumn("Kkt_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Kkt_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

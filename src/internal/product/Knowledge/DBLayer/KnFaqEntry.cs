using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

[SooTable("KnFaqEntry", Caption = "FAQ 条目")]
public class KnFaqEntry
{
    [SooColumn("Kfe_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kfe_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kfe_KnowledgeBaseId", Length = 36, CanBeNull = false)]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    [SooColumn("Kfe_Question", Length = 1024, CanBeNull = false)]
    public string Question { get; set; } = string.Empty;

    [SooColumn("Kfe_Answer", Length = -1, CanBeNull = false)]
    public string Answer { get; set; } = string.Empty;

    [SooColumn("Kfe_TagId", Length = 36, CanBeNull = true)]
    public string? TagId { get; set; }

    [SooColumn("Kfe_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Kfe_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Kfe_UpdateTime", CanBeNull = false)]
    public DateTime UpdateTime { get; set; }
}

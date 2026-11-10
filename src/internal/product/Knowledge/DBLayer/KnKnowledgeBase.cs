using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

/// <summary>F2 精简知识库表（对齐上游关键列；完整 schema 后续跟 migrations）。</summary>
[SooTable("KnKnowledgeBase", Caption = "知识库")]
public class KnKnowledgeBase
{
    [SooColumn("Kkb_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kkb_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kkb_Name", Length = 256, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Kkb_Description", Length = 1024, CanBeNull = true)]
    public string? Description { get; set; }

    /// <summary>document | faq | wiki</summary>
    [SooColumn("Kkb_Type", Length = 32, CanBeNull = false)]
    public string Type { get; set; } = KnowledgeBaseTypes.Document;

    [SooColumn("Kkb_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

public static class KnowledgeBaseTypes
{
    public const string Document = "document";
    public const string Faq = "faq";
    public const string Wiki = "wiki";
}

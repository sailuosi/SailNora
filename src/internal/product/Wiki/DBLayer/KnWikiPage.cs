using mooSQL.data;

namespace SailNora.Product.Wiki.DBLayer;

[SooTable("KnWikiPage", Caption = "Wiki 页面")]
public class KnWikiPage
{
    [SooColumn("Kwp_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kwp_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kwp_KnowledgeBaseId", Length = 36, CanBeNull = true)]
    public string? KnowledgeBaseId { get; set; }

    [SooColumn("Kwp_SourceKnowledgeId", Length = 36, CanBeNull = true)]
    public string? SourceKnowledgeId { get; set; }

    [SooColumn("Kwp_Slug", Length = 128, CanBeNull = false)]
    public string Slug { get; set; } = string.Empty;

    [SooColumn("Kwp_Title", Length = 256, CanBeNull = false)]
    public string Title { get; set; } = string.Empty;

    [SooColumn("Kwp_Content", Length = -1, CanBeNull = false)]
    public string Content { get; set; } = string.Empty;

    [SooColumn("Kwp_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Kwp_UpdateTime", CanBeNull = false)]
    public DateTime UpdateTime { get; set; }
}

using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

[SooTable("KnChunkRevision", Caption = "分块修订历史")]
public class KnChunkRevision
{
    [SooColumn("Kcr_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kcr_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kcr_ChunkId", Length = 36, CanBeNull = false)]
    public string ChunkId { get; set; } = string.Empty;

    [SooColumn("Kcr_KnowledgeId", Length = 36, CanBeNull = false)]
    public string KnowledgeId { get; set; } = string.Empty;

    [SooColumn("Kcr_Revision", CanBeNull = false)]
    public int Revision { get; set; }

    [SooColumn("Kcr_Content", Length = -1, CanBeNull = false)]
    public string Content { get; set; } = string.Empty;

    [SooColumn("Kcr_EmbeddingJson", Length = -1, CanBeNull = true)]
    public string? EmbeddingJson { get; set; }

    [SooColumn("Kcr_EditedAt", CanBeNull = false)]
    public DateTime EditedAt { get; set; }
}

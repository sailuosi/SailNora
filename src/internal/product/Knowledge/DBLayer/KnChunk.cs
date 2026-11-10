using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

[SooTable("KnChunk", Caption = "知识分块")]
public class KnChunk
{
    [SooColumn("Kc_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kc_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kc_KnowledgeBaseId", Length = 36, CanBeNull = false)]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    [SooColumn("Kc_KnowledgeId", Length = 36, CanBeNull = false)]
    public string KnowledgeId { get; set; } = string.Empty;

    [SooColumn("Kc_Content", Length = -1, CanBeNull = false)]
    public string Content { get; set; } = string.Empty;

    [SooColumn("Kc_ChunkIndex", CanBeNull = false)]
    public int ChunkIndex { get; set; }

    [SooColumn("Kc_ChunkType", Length = 32, CanBeNull = false)]
    public string ChunkType { get; set; } = "text";

    [SooColumn("Kc_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

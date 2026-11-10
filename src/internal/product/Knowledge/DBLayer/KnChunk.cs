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

    /// <summary>JSON float[]；无 pgvector 时先落库供应用层余弦检索。</summary>
    [SooColumn("Kc_EmbeddingJson", Length = -1, CanBeNull = true)]
    public string? EmbeddingJson { get; set; }

    /// <summary>当前内容修订号；每次用户编辑或回滚自增。</summary>
    [SooColumn("Kc_ContentRevision", CanBeNull = false)]
    public int ContentRevision { get; set; }

    [SooColumn("Kc_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

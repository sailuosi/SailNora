using mooSQL.data;

namespace SailNora.Product.Knowledge.DBLayer;

[SooTable("KnKnowledge", Caption = "知识文档")]
public class KnKnowledge
{
    [SooColumn("Kk_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kk_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kk_KnowledgeBaseId", Length = 36, CanBeNull = false)]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    [SooColumn("Kk_Type", Length = 32, CanBeNull = false)]
    public string Type { get; set; } = "file";

    [SooColumn("Kk_Title", Length = 512, CanBeNull = false)]
    public string Title { get; set; } = string.Empty;

    [SooColumn("Kk_FileName", Length = 512, CanBeNull = true)]
    public string? FileName { get; set; }

    [SooColumn("Kk_FileType", Length = 64, CanBeNull = true)]
    public string? FileType { get; set; }

    [SooColumn("Kk_FileSize", CanBeNull = false)]
    public long FileSize { get; set; }

    [SooColumn("Kk_FilePath", Length = 1024, CanBeNull = true)]
    public string? FilePath { get; set; }

    [SooColumn("Kk_FileHash", Length = 128, CanBeNull = true)]
    public string? FileHash { get; set; }

    /// <summary>pending | processing | completed | failed</summary>
    [SooColumn("Kk_ParseStatus", Length = 32, CanBeNull = false)]
    public string ParseStatus { get; set; } = ParseStatuses.Pending;

    [SooColumn("Kk_ErrorMessage", Length = 2048, CanBeNull = true)]
    public string? ErrorMessage { get; set; }

    [SooColumn("Kk_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Kk_ProcessedAt", CanBeNull = true)]
    public DateTime? ProcessedAt { get; set; }
}

public static class ParseStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

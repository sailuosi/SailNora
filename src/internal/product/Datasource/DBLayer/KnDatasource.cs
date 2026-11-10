using mooSQL.data;

namespace SailNora.Product.Datasource.DBLayer;

[SooTable("KnDatasource", Caption = "数据源")]
public class KnDatasource
{
    [SooColumn("Kds_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Kds_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Kds_KnowledgeBaseId", Length = 36, CanBeNull = false)]
    public string KnowledgeBaseId { get; set; } = string.Empty;

    [SooColumn("Kds_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>rss | gitlab | …</summary>
    [SooColumn("Kds_Type", Length = 32, CanBeNull = false)]
    public string Type { get; set; } = DatasourceTypes.Rss;

    [SooColumn("Kds_Url", Length = 1024, CanBeNull = false)]
    public string Url { get; set; } = string.Empty;

    [SooColumn("Kds_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    /// <summary>idle | syncing | completed | failed</summary>
    [SooColumn("Kds_SyncStatus", Length = 32, CanBeNull = false)]
    public string SyncStatus { get; set; } = DatasourceSyncStatuses.Idle;

    [SooColumn("Kds_LastSyncAt", CanBeNull = true)]
    public DateTime? LastSyncAt { get; set; }

    [SooColumn("Kds_LastError", Length = 2048, CanBeNull = true)]
    public string? LastError { get; set; }

    [SooColumn("Kds_ImportedCount", CanBeNull = false)]
    public int ImportedCount { get; set; }

    [SooColumn("Kds_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

public static class DatasourceTypes
{
    public const string Rss = "rss";
    public const string Gitlab = "gitlab";
    public const string Confluence = "confluence";
    public const string Feishu = "feishu";
}

public static class DatasourceSyncStatuses
{
    public const string Idle = "idle";
    public const string Syncing = "syncing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

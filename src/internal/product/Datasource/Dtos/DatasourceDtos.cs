namespace SailNora.Product.Datasource.Dtos;

public sealed class CreateDatasourceInput
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "rss";
    public string Url { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
}

public sealed class DatasourceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string SyncStatus { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public string? LastError { get; set; }
    public int ImportedCount { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class DatasourceSyncPayload
{
    public string RequestId { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public string DatasourceId { get; set; } = string.Empty;
}

public sealed class DatasourceSyncResultDto
{
    public string DatasourceId { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = string.Empty;
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public string? Error { get; set; }
}

public static class DatasourceTaskTypes
{
    public const string DatasourceSync = "datasource:sync";
}

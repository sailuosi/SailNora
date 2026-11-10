namespace SailNora.Product.Knowledge.Dtos;

public sealed class CreateKnowledgeBaseInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "document";
}

public sealed class KnowledgeBaseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SpaceId { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

public sealed class KnowledgeDto
{
    public string Id { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public string ParseStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class ChunkDto
{
    public string Id { get; set; } = string.Empty;
    public string KnowledgeId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string ChunkType { get; set; } = "text";
}

public sealed class DocumentProcessPayload
{
    public string RequestId { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public string KnowledgeId { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
}

/// <summary>与上游 asynq 任务类型对齐，便于对照。</summary>
public static class KnowledgeTaskTypes
{
    public const string DocumentProcess = "document:process";
}

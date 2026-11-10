namespace SailNora.Product.Memory.Dtos;

public sealed class MemoryDto
{
    public string Id { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

public sealed class MemoryExtractPayload
{
    public string RequestId { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public Guid UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
}

public sealed class MemoryExtractResultDto
{
    public string SessionId { get; set; } = string.Empty;
    public int Extracted { get; set; }
    public int Skipped { get; set; }
    /// <summary>heuristic | llm</summary>
    public string Mode { get; set; } = "heuristic";
}

public static class MemoryTaskTypes
{
    public const string MemoryExtract = "memory:extract";
}

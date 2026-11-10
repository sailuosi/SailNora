namespace SailNora.Product.Wiki.Dtos;

public sealed class WikiIngestPayload
{
    public string RequestId { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public string KnowledgeId { get; set; } = string.Empty;
    public string? KnowledgeBaseId { get; set; }
}

public sealed class WikiIngestRequest
{
    public string KnowledgeId { get; set; } = string.Empty;
}

public sealed class WikiIngestResultDto
{
    public string KnowledgeId { get; set; } = string.Empty;
    public string WikiPageId { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}

public static class WikiTaskTypes
{
    public const string WikiIngest = "wiki:ingest";
}

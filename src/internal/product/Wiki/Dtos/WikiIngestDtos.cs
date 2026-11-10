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

public sealed class WikiFinalizePayload
{
    public string RequestId { get; set; } = string.Empty;
    public Guid SpaceId { get; set; }
    public string? KnowledgeBaseId { get; set; }
}

public sealed class WikiFinalizeRequest
{
    public string? KnowledgeBaseId { get; set; }
}

public sealed class WikiFinalizeResultDto
{
    public int FinalizedCount { get; set; }
    public string IndexPageId { get; set; } = string.Empty;
    public string IndexSlug { get; set; } = "_index";
}

public static class WikiTaskTypes
{
    public const string WikiIngest = "wiki:ingest";
    public const string WikiFinalize = "wiki:finalize";
}

public static class WikiPageStatuses
{
    public const string Draft = "draft";
    public const string Finalized = "finalized";
}

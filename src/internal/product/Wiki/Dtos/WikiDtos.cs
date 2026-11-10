namespace SailNora.Product.Wiki.Dtos;

public sealed class CreateWikiPageInput
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? KnowledgeBaseId { get; set; }
}

public sealed class UpdateWikiPageInput
{
    public string? Title { get; set; }
    public string? Content { get; set; }
}

public sealed class WikiPageDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? KnowledgeBaseId { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public sealed class WikiPageSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTime UpdateTime { get; set; }
}

public sealed class WikiGraphNodeDto
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int LinkCount { get; set; }
}

public sealed class WikiGraphEdgeDto
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
}

public sealed class WikiGraphMetaDto
{
    public string Mode { get; set; } = "overview";
    public int Total { get; set; }
    public int Returned { get; set; }
    public bool Truncated { get; set; }
}

public sealed class WikiGraphDataDto
{
    public List<WikiGraphNodeDto> Nodes { get; set; } = [];
    public List<WikiGraphEdgeDto> Edges { get; set; } = [];
    public WikiGraphMetaDto Meta { get; set; } = new();
}

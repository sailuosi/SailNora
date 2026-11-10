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
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public sealed class WikiPageSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime UpdateTime { get; set; }
}

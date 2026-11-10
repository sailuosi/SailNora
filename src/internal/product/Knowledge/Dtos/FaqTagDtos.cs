namespace SailNora.Product.Knowledge.Dtos;

public sealed class CreateTagInput
{
    public string Name { get; set; } = string.Empty;
}

public sealed class KnowledgeTagDto
{
    public string Id { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}

public sealed class CreateFaqEntryInput
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? TagId { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class UpdateFaqEntryInput
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? TagId { get; set; }
    public bool? Enabled { get; set; }
}

public sealed class FaqEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string KnowledgeBaseId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string? TagId { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

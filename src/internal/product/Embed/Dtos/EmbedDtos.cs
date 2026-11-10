namespace SailNora.Product.Embed.Dtos;

public sealed class CreateEmbedChannelInput
{
    public string Name { get; set; } = string.Empty;
    public List<string>? KnowledgeBaseIds { get; set; }
}

public sealed class EmbedChannelDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    /// <summary>仅创建/轮换时返回明文。</summary>
    public string? Token { get; set; }
    public List<string> KnowledgeBaseIds { get; set; } = [];
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class EmbedConfigDto
{
    public string ChannelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> KnowledgeBaseIds { get; set; } = [];
}

public sealed class CreateEmbedSessionInput
{
    public string? Title { get; set; }
}

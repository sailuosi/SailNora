namespace SailNora.Product.Im.Dtos;

public sealed class CreateImChannelInput
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "generic";
    public string? KnowledgeBaseId { get; set; }
}

public sealed class ImChannelDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    /// <summary>仅创建时返回明文密钥。</summary>
    public string? Secret { get; set; }
    public string? KnowledgeBaseId { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class ImWebhookBody
{
    public string? Text { get; set; }
    public string? UserId { get; set; }
    public string? Challenge { get; set; }
}

public sealed class ImWebhookResultDto
{
    public bool Ok { get; set; }
    public string? SessionId { get; set; }
    public string? EventId { get; set; }
    public string? Challenge { get; set; }
}

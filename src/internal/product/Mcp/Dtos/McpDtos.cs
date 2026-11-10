namespace SailNora.Product.Mcp.Dtos;

public sealed class CreateMcpServiceInput
{
    public string Name { get; set; } = string.Empty;
    public string Transport { get; set; } = "sse";
    public string Url { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
}

public sealed class McpServiceDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool RequireApproval { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class CreateMcpEndpointInput
{
    public string Name { get; set; } = string.Empty;
}

public sealed class McpEndpointDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TokenPrefix { get; set; } = string.Empty;
    /// <summary>仅创建/轮换时返回明文。</summary>
    public string? Token { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreateTime { get; set; }
}

public sealed class McpToolInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequireApproval { get; set; }
}

public sealed class ResolveHitlInput
{
    public string Decision { get; set; } = "approve"; // approve | reject
    public string? Reason { get; set; }
}

public sealed class HitlPendingDto
{
    public string Id { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public string? ArgumentsJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

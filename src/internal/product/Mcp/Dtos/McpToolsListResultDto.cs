namespace SailNora.Product.Mcp.Dtos;

public sealed class McpToolsListResultDto
{
    public List<McpToolInfoDto> Tools { get; set; } = [];
    public bool Cached { get; set; }
    public DateTime? CachedAt { get; set; }
    public int TtlSeconds { get; set; }
}

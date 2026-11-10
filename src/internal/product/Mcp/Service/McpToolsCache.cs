using System.Collections.Concurrent;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.Dtos;

namespace SailNora.Product.Mcp.Service;

/// <summary>F5n：出站 MCP <c>tools/list</c> 进程内 TTL 缓存。</summary>
public sealed class McpToolsCache : ISingleton
{
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public TimeSpan Ttl { get; set; } = DefaultTtl;

    public bool TryGet(string serviceId, out IReadOnlyList<McpToolInfoDto> tools, out DateTime cachedAt)
    {
        tools = [];
        cachedAt = default;
        if (!_entries.TryGetValue(serviceId, out var entry))
            return false;
        if (DateTime.UtcNow - entry.CachedAtUtc > Ttl)
        {
            _entries.TryRemove(serviceId, out _);
            return false;
        }

        tools = entry.Tools.Select(Clone).ToList();
        cachedAt = entry.CachedAtUtc;
        return true;
    }

    public void Set(string serviceId, IEnumerable<McpToolInfoDto> tools)
    {
        var snapshot = tools.Select(Clone).ToList();
        _entries[serviceId] = new Entry(snapshot, DateTime.UtcNow);
    }

    public bool Invalidate(string serviceId) => _entries.TryRemove(serviceId, out _);

    public void InvalidateAll() => _entries.Clear();

    private static McpToolInfoDto Clone(McpToolInfoDto t) => new()
    {
        Name = t.Name,
        Description = t.Description,
        RequireApproval = t.RequireApproval
    };

    private sealed record Entry(List<McpToolInfoDto> Tools, DateTime CachedAtUtc);
}

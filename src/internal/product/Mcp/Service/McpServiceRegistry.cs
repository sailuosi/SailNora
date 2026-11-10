using System.Security.Cryptography;
using System.Text;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.DBLayer;
using SailNora.Product.Mcp.Dtos;

namespace SailNora.Product.Mcp.Service;

[Scoped]
public sealed class McpServiceRegistry
{
    private readonly MooRepository<KnMcpService> _services;
    private readonly MooRepository<KnMcpEndpoint> _endpoints;
    private readonly McpOutboundDialer _dialer;
    private readonly McpToolsCache _toolsCache;

    public McpServiceRegistry(
        MooRepository<KnMcpService> services,
        MooRepository<KnMcpEndpoint> endpoints,
        McpOutboundDialer dialer,
        McpToolsCache toolsCache)
    {
        _services = services;
        _endpoints = endpoints;
        _dialer = dialer;
        _toolsCache = toolsCache;
    }

    public McpServiceDto CreateService(Guid spaceId, CreateMcpServiceInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");
        if (string.IsNullOrWhiteSpace(input.Url))
            throw new InvalidOperationException("URL 不能为空");

        var entity = new KnMcpService
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            Transport = string.IsNullOrWhiteSpace(input.Transport) ? "sse" : input.Transport.Trim(),
            Url = input.Url.Trim(),
            AuthToken = string.IsNullOrWhiteSpace(input.AuthToken) ? null : input.AuthToken.Trim(),
            Enabled = true,
            RequireApproval = input.RequireApproval,
            CreateTime = DateTime.Now
        };
        _services.Repo.Insert(entity);
        return ToServiceDto(entity);
    }

    public List<McpServiceDto> ListServices(Guid spaceId) =>
        _services.Repo.GetList(s => s.SpaceId == spaceId)
            .OrderByDescending(s => s.CreateTime)
            .Select(ToServiceDto)
            .ToList();

    public McpServiceDto? GetService(Guid spaceId, string id)
    {
        var s = _services.Repo.GetById(id);
        if (s is null || s.SpaceId != spaceId)
            return null;
        return ToServiceDto(s);
    }

    public void DeleteService(Guid spaceId, string id)
    {
        var s = _services.Repo.GetById(id)
                ?? throw new InvalidOperationException("MCP 服务不存在");
        if (s.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        _toolsCache.Invalidate(id);
        _services.Repo.Delete(s);
    }

    public async Task<McpToolsListResultDto> ListServiceToolsAsync(
        Guid spaceId,
        string id,
        bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        var entity = RequireServiceEntity(spaceId, id);
        var ttlSec = (int)_toolsCache.Ttl.TotalSeconds;

        if (!McpOutboundDialer.IsHttpTransport(entity.Transport))
        {
            var stub =
                new List<McpToolInfoDto>
                {
                    new()
                    {
                        Name = "echo",
                        Description = $"Stub tool for transport '{entity.Transport}' (no dial)",
                        RequireApproval = entity.RequireApproval
                    }
                };
            return new McpToolsListResultDto { Tools = stub, Cached = false, TtlSeconds = ttlSec };
        }

        if (!refresh && _toolsCache.TryGet(id, out var cached, out var cachedAt))
        {
            foreach (var t in cached)
                t.RequireApproval = entity.RequireApproval;
            return new McpToolsListResultDto
            {
                Tools = cached.ToList(),
                Cached = true,
                CachedAt = cachedAt.ToLocalTime(),
                TtlSeconds = ttlSec
            };
        }

        var dial = await _dialer.ProbeAsync(entity.Transport, entity.Url, entity.AuthToken, cancellationToken);
        if (!dial.Ok)
            throw new InvalidOperationException(dial.Message);

        foreach (var t in dial.Tools)
            t.RequireApproval = entity.RequireApproval;
        _toolsCache.Set(id, dial.Tools);
        return new McpToolsListResultDto
        {
            Tools = dial.Tools,
            Cached = false,
            CachedAt = DateTime.Now,
            TtlSeconds = ttlSec
        };
    }

    public object InvalidateToolsCache(Guid spaceId, string id)
    {
        _ = RequireServiceEntity(spaceId, id);
        var removed = _toolsCache.Invalidate(id);
        return new { service_id = id, invalidated = removed };
    }

    public async Task<object> TestServiceAsync(
        Guid spaceId, string id, CancellationToken cancellationToken = default)
    {
        var entity = RequireServiceEntity(spaceId, id);
        var dial = await _dialer.ProbeAsync(entity.Transport, entity.Url, entity.AuthToken, cancellationToken);
        if (dial.Ok)
        {
            foreach (var t in dial.Tools)
                t.RequireApproval = entity.RequireApproval;
            _toolsCache.Set(id, dial.Tools);
        }

        return new
        {
            ok = dial.Ok,
            service_id = entity.Id,
            transport = entity.Transport,
            protocol_version = dial.ProtocolVersion,
            server_name = dial.ServerName,
            tools = dial.Tools.Select(t => t.Name).ToList(),
            message = dial.Message,
            tools_cached = dial.Ok
        };
    }

    public async Task<McpCallToolResultDto> CallServiceToolAsync(
        Guid spaceId,
        string id,
        McpCallToolInput input,
        CancellationToken cancellationToken = default)
    {
        var entity = RequireServiceEntity(spaceId, id);
        if (!entity.Enabled)
            throw new InvalidOperationException("MCP 服务未启用");
        if (string.IsNullOrWhiteSpace(input.ToolName))
            throw new InvalidOperationException("toolName 不能为空");

        return await _dialer.CallToolAsync(
            entity.Transport,
            entity.Url,
            input.ToolName,
            input.Arguments,
            entity.AuthToken,
            cancellationToken);
    }

    private KnMcpService RequireServiceEntity(Guid spaceId, string id)
    {
        var s = _services.Repo.GetById(id)
                ?? throw new InvalidOperationException("MCP 服务不存在");
        if (s.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        return s;
    }

    public McpEndpointDto CreateEndpoint(Guid spaceId, CreateMcpEndpointInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("名称不能为空");

        var token = "mcp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var entity = new KnMcpEndpoint
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            TokenHash = HashToken(token),
            TokenPrefix = token[..8],
            Enabled = true,
            CreateTime = DateTime.Now
        };
        _endpoints.Repo.Insert(entity);
        var dto = ToEndpointDto(entity);
        dto.Token = token;
        return dto;
    }

    public List<McpEndpointDto> ListEndpoints(Guid spaceId) =>
        _endpoints.Repo.GetList(e => e.SpaceId == spaceId)
            .OrderByDescending(e => e.CreateTime)
            .Select(ToEndpointDto)
            .ToList();

    public McpEndpointDto RotateEndpointToken(Guid spaceId, string id)
    {
        var e = _endpoints.Repo.GetById(id)
                ?? throw new InvalidOperationException("Endpoint 不存在");
        if (e.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        var token = "mcp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        e.TokenHash = HashToken(token);
        e.TokenPrefix = token[..8];
        _endpoints.Repo.Update(e);
        var dto = ToEndpointDto(e);
        dto.Token = token;
        return dto;
    }

    public void DeleteEndpoint(Guid spaceId, string id)
    {
        var e = _endpoints.Repo.GetById(id)
                ?? throw new InvalidOperationException("Endpoint 不存在");
        if (e.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");
        _endpoints.Repo.Delete(e);
    }

    public KnMcpEndpoint? AuthenticateEndpoint(string endpointId, string bearerToken)
    {
        var e = _endpoints.Repo.GetById(endpointId);
        if (e is null || !e.Enabled)
            return null;
        if (!string.Equals(e.TokenHash, HashToken(bearerToken), StringComparison.Ordinal))
            return null;
        return e;
    }

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static McpServiceDto ToServiceDto(KnMcpService s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Transport = s.Transport,
        Url = s.Url,
        Enabled = s.Enabled,
        RequireApproval = s.RequireApproval,
        CreateTime = s.CreateTime
    };

    private static McpEndpointDto ToEndpointDto(KnMcpEndpoint e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        TokenPrefix = e.TokenPrefix,
        Enabled = e.Enabled,
        CreateTime = e.CreateTime
    };
}

using System.Security.Cryptography;
using System.Text;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.DBLayer;
using SailNora.Product.Mcp.Dtos;

namespace SailNora.Product.Mcp.Service;

public sealed class McpServiceRegistry : IScoped
{
    private readonly MooRepository<KnMcpService> _services;
    private readonly MooRepository<KnMcpEndpoint> _endpoints;

    public McpServiceRegistry(
        MooRepository<KnMcpService> services,
        MooRepository<KnMcpEndpoint> endpoints)
    {
        _services = services;
        _endpoints = endpoints;
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
        _services.Repo.Delete(s);
    }

    /// <summary>F5c：尚未真实拨号，返回占位工具目录。</summary>
    public List<McpToolInfoDto> ListServiceTools(Guid spaceId, string id)
    {
        var s = GetService(spaceId, id)
                ?? throw new InvalidOperationException("MCP 服务不存在");
        return
        [
            new McpToolInfoDto
            {
                Name = "echo",
                Description = $"Placeholder tool for MCP service '{s.Name}'",
                RequireApproval = s.RequireApproval
            }
        ];
    }

    public object TestService(Guid spaceId, string id)
    {
        var s = GetService(spaceId, id)
                ?? throw new InvalidOperationException("MCP 服务不存在");
        return new
        {
            ok = true,
            service_id = s.Id,
            transport = s.Transport,
            message = "F5c stub: outbound MCP dial not wired yet"
        };
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

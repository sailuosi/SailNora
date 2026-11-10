using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Product.Mcp.Service;

namespace SailNora.Product.Mcp.Controller;

/// <summary>入站 MCP JSON-RPC（不加 ApiResult 包装）。</summary>
[ApiController]
[AllowAnonymous]
public sealed class McpInboundController : ControllerBase
{
    private readonly McpServiceRegistry _mcp;

    public McpInboundController(McpServiceRegistry mcp) => _mcp = mcp;

    [HttpPost("/mcp/{endpointId}")]
    public async Task<IActionResult> Inbound(string endpointId, CancellationToken cancellationToken)
    {
        var auth = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new { error = "missing bearer token" });

        var token = auth["Bearer ".Length..].Trim();
        var endpoint = _mcp.AuthenticateEndpoint(endpointId, token);
        if (endpoint is null)
            return Unauthorized(new { error = "invalid token" });

        using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var method = root.TryGetProperty("method", out var m) ? m.GetString() : null;
        object? id = null;
        if (root.TryGetProperty("id", out var idEl))
        {
            id = idEl.ValueKind switch
            {
                JsonValueKind.Number => idEl.TryGetInt64(out var n) ? n : idEl.GetDouble(),
                JsonValueKind.String => idEl.GetString(),
                _ => null
            };
        }

        object result = method switch
        {
            "initialize" => new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { tools = new { } },
                serverInfo = new { name = "sail-nora", version = "0.1", endpoint = endpoint.Name }
            },
            "tools/list" => new
            {
                tools = new[]
                {
                    new
                    {
                        name = "search_knowledge",
                        description = "Search sail-nora knowledge bases (inbound stub)",
                        inputSchema = new
                        {
                            type = "object",
                            properties = new { query = new { type = "string" } }
                        }
                    }
                }
            },
            "ping" => new { },
            _ => new { message = $"F5c stub method '{method ?? "(null)"}'" }
        };

        return Ok(new { jsonrpc = "2.0", id, result });
    }
}

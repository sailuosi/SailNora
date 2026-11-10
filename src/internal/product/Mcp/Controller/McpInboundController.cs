using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailNora.Product.Agent.Tools;
using SailNora.Product.Mcp.DBLayer;
using SailNora.Product.Mcp.Service;

namespace SailNora.Product.Mcp.Controller;

/// <summary>入站 MCP JSON-RPC（不加 ApiResult 包装）。</summary>
[ApiController]
[AllowAnonymous]
public sealed class McpInboundController : ControllerBase
{
    private readonly McpServiceRegistry _mcp;
    private readonly KnowledgeAgentTools _tools;

    public McpInboundController(McpServiceRegistry mcp, KnowledgeAgentTools tools)
    {
        _mcp = mcp;
        _tools = tools;
    }

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
            "initialize" => BuildInitialize(endpoint.Name),
            "tools/list" => BuildToolsList(),
            "tools/call" => await HandleToolsCallAsync(endpoint, root, cancellationToken),
            "ping" => new Dictionary<string, object>(),
            _ => new Dictionary<string, object?> { ["message"] = $"F5c stub method '{method ?? "(null)"}'" }
        };

        return Ok(new { jsonrpc = "2.0", id, result });
    }

    /// <summary>
    /// F5j/F5l：MCP SSE 握手 — <c>event: endpoint</c> 后按间隔心跳（comment + ping 事件）。
    /// 查询参数：<c>interval_sec</c>（默认 5）、<c>max_sec</c>（默认 60，0=直到客户端断开，上限 600）。
    /// </summary>
    [HttpGet("/mcp/{endpointId}/sse")]
    public async Task Sse(string endpointId, CancellationToken cancellationToken)
    {
        var auth = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";
            await Response.WriteAsync("""{"error":"missing bearer token"}""", cancellationToken);
            return;
        }

        var token = auth["Bearer ".Length..].Trim();
        var endpoint = _mcp.AuthenticateEndpoint(endpointId, token);
        if (endpoint is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";
            await Response.WriteAsync("""{"error":"invalid token"}""", cancellationToken);
            return;
        }

        var intervalSec = 5;
        if (int.TryParse(Request.Query["interval_sec"], out var iq) && iq > 0)
            intervalSec = Math.Clamp(iq, 1, 60);
        var maxSec = 60;
        if (int.TryParse(Request.Query["max_sec"], out var mq))
            maxSec = mq <= 0 ? 0 : Math.Clamp(mq, 1, 600);

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream; charset=utf-8";
        HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

        var postUrl = $"{Request.Scheme}://{Request.Host}/mcp/{endpointId}";
        var payload = JsonSerializer.Serialize(new { url = postUrl });
        var endpointBytes = Encoding.UTF8.GetBytes($"event: endpoint\ndata: {payload}\n\n");
        await Response.Body.WriteAsync(endpointBytes, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (maxSec > 0)
            linked.CancelAfter(TimeSpan.FromSeconds(maxSec));

        var seq = 0;
        try
        {
            while (!linked.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSec), linked.Token);
                seq++;
                // SSE comment keep-alive + 显式 ping 事件（便于冒烟计数）
                var ping = Encoding.UTF8.GetBytes($": keepalive {seq}\nevent: ping\ndata: {{\"seq\":{seq}}}\n\n");
                await Response.Body.WriteAsync(ping, linked.Token);
                await Response.Body.FlushAsync(linked.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // 客户端断开或 max_sec 到期
        }
    }

    private static object BuildInitialize(string endpointName) => new
    {
        protocolVersion = "2024-11-05",
        capabilities = new { tools = new { } },
        serverInfo = new { name = "sail-nora", version = "0.1", endpoint = endpointName }
    };

    private static object BuildToolsList() => new
    {
        tools = new object[]
        {
            new
            {
                name = "search_knowledge",
                description = "Search sail-nora knowledge bases via hybrid retrieval",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string" },
                        limit = new { type = "integer" },
                        knowledge_base_ids = new { type = "array", items = new { type = "string" } }
                    },
                    required = new[] { "query" }
                }
            },
            new
            {
                name = "echo",
                description = "Echo arguments back as text",
                inputSchema = new
                {
                    type = "object",
                    properties = new { message = new { type = "string" } }
                }
            }
        }
    };

    private async Task<object> HandleToolsCallAsync(
        KnMcpEndpoint endpoint,
        JsonElement root,
        CancellationToken cancellationToken)
    {
        string? name = null;
        JsonElement? args = null;
        if (root.TryGetProperty("params", out var p))
        {
            if (p.TryGetProperty("name", out var n))
                name = n.GetString();
            if (p.TryGetProperty("arguments", out var a))
                args = a.Clone();
        }

        if (string.Equals(name, "echo", StringComparison.OrdinalIgnoreCase))
        {
            var message = "echo";
            if (args is { } a && a.TryGetProperty("message", out var msg))
                message = msg.GetString() ?? message;
            return new
            {
                content = new[] { new { type = "text", text = message } },
                isError = false
            };
        }

        if (string.Equals(name, "search_knowledge", StringComparison.OrdinalIgnoreCase))
        {
            var query = "";
            var topK = 5;
            List<string>? kbIds = null;
            if (args is { } a)
            {
                if (a.TryGetProperty("query", out var q))
                    query = q.GetString() ?? "";
                if (a.TryGetProperty("limit", out var lim) && lim.TryGetInt32(out var n) && n > 0)
                    topK = Math.Clamp(n, 1, 20);
                else if (a.TryGetProperty("top_k", out var tk) && tk.TryGetInt32(out var n2) && n2 > 0)
                    topK = Math.Clamp(n2, 1, 20);
                if (a.TryGetProperty("knowledge_base_ids", out var kb) && kb.ValueKind == JsonValueKind.Array)
                    kbIds = kb.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList();
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return new
                {
                    content = new[] { new { type = "text", text = "query is required" } },
                    isError = true
                };
            }

            var search = await _tools.SearchKnowledgeAsync(
                endpoint.SpaceId,
                query.Trim(),
                kbIds,
                knowledgeIds: null,
                topK,
                cancellationToken);
            return new
            {
                content = new[] { new { type = "text", text = KnowledgeAgentTools.ToJson(search) } },
                isError = false
            };
        }

        return new
        {
            content = new[] { new { type = "text", text = $"unknown tool '{name ?? "(null)"}'" } },
            isError = true
        };
    }
}

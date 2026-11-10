using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.Dtos;

namespace SailNora.Product.Mcp.Service;

/// <summary>F5f：出站 MCP JSON-RPC dial（http/sse/streamable → POST；stdio stub）。</summary>
public sealed class McpOutboundDialer : IScoped
{
    private static readonly HashSet<string> HttpTransports = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "sse", "streamable", "http-streamable", "streamable-http"
    };

    private readonly IHttpClientFactory _httpClientFactory;

    public McpOutboundDialer(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public static bool IsHttpTransport(string? transport) =>
        !string.IsNullOrWhiteSpace(transport) && HttpTransports.Contains(transport.Trim());

    public async Task<McpDialResult> ProbeAsync(
        string transport,
        string url,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsHttpTransport(transport))
        {
            return new McpDialResult
            {
                Ok = false,
                Message = $"transport '{transport}' outbound dial not wired (F5f http/sse only)",
                Tools = []
            };
        }

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new McpDialResult { Ok = false, Message = "URL 无效（仅 http/https）", Tools = [] };
        }

        try
        {
            var client = _httpClientFactory.CreateClient("mcp-outbound");
            var init = await PostJsonRpcAsync(client, uri, 1, "initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "sail-nora", version = "0.1" }
            }, bearerToken, cancellationToken);

            if (!init.Ok)
                return init;

            // 部分服务端需要 initialized 通知；忽略失败
            _ = await PostJsonRpcAsync(client, uri, null, "notifications/initialized", new { }, bearerToken, cancellationToken, notification: true);

            var list = await PostJsonRpcAsync(client, uri, 2, "tools/list", new { }, bearerToken, cancellationToken);
            if (!list.Ok)
                return list;

            return new McpDialResult
            {
                Ok = true,
                Message = $"dial ok protocol={init.ProtocolVersion ?? "unknown"} tools={list.Tools.Count}",
                ProtocolVersion = init.ProtocolVersion,
                ServerName = init.ServerName,
                Tools = list.Tools
            };
        }
        catch (Exception ex)
        {
            return new McpDialResult { Ok = false, Message = ex.Message, Tools = [] };
        }
    }

    public async Task<McpCallToolResultDto> CallToolAsync(
        string transport,
        string url,
        string toolName,
        Dictionary<string, object?>? arguments,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return FailCall(toolName, "toolName 不能为空");
        if (!IsHttpTransport(transport))
            return FailCall(toolName, $"transport '{transport}' tools/call not wired");
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return FailCall(toolName, "URL 无效（仅 http/https）");

        try
        {
            var client = _httpClientFactory.CreateClient("mcp-outbound");
            // 轻量会话：initialize 后再 call（部分服务端要求）
            var init = await PostJsonRpcAsync(client, uri, 1, "initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "sail-nora", version = "0.1" }
            }, bearerToken, cancellationToken);
            if (!init.Ok)
                return FailCall(toolName, init.Message);

            _ = await PostJsonRpcAsync(client, uri, null, "notifications/initialized", new { }, bearerToken, cancellationToken, notification: true);

            var call = await PostJsonRpcRawAsync(client, uri, 3, "tools/call", new
            {
                name = toolName.Trim(),
                arguments = arguments ?? new Dictionary<string, object?>()
            }, bearerToken, cancellationToken);

            if (!call.Ok)
                return FailCall(toolName, call.ErrorMessage ?? "tools/call failed");

            var content = new List<McpCallToolContentDto>();
            var isError = false;
            if (call.Result is JsonElement result)
            {
                if (result.TryGetProperty("isError", out var ie) &&
                    (ie.ValueKind == JsonValueKind.True || (ie.ValueKind == JsonValueKind.String && ie.GetString() == "true")))
                    isError = true;

                if (result.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in contentEl.EnumerateArray())
                    {
                        content.Add(new McpCallToolContentDto
                        {
                            Type = c.TryGetProperty("type", out var t) ? t.GetString() ?? "text" : "text",
                            Text = c.TryGetProperty("text", out var tx) ? tx.GetString() ?? "" : c.ToString()
                        });
                    }
                }
            }

            return new McpCallToolResultDto
            {
                Ok = !isError,
                IsError = isError,
                ToolName = toolName.Trim(),
                Content = content,
                Message = isError ? "tool reported isError" : "ok"
            };
        }
        catch (Exception ex)
        {
            return FailCall(toolName, ex.Message);
        }
    }

    private static McpCallToolResultDto FailCall(string? toolName, string message) => new()
    {
        Ok = false,
        IsError = true,
        ToolName = toolName ?? "",
        Content = [],
        Message = message
    };

    private static async Task<McpDialResult> PostJsonRpcAsync(
        HttpClient client,
        Uri uri,
        object? id,
        string method,
        object? paramsObj,
        string? bearerToken,
        CancellationToken cancellationToken,
        bool notification = false)
    {
        object payload = notification
            ? new { jsonrpc = "2.0", method, @params = paramsObj }
            : new { jsonrpc = "2.0", id, method, @params = paramsObj };

        using var req = new HttpRequestMessage(HttpMethod.Post, uri);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(bearerToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

        using var resp = await client.SendAsync(req, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (notification)
            return new McpDialResult { Ok = true, Message = "notification sent", Tools = [] };

        if (!resp.IsSuccessStatusCode)
            return new McpDialResult
            {
                Ok = false,
                Message = $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}",
                Tools = []
            };

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var root = doc.RootElement;
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var em) ? em.GetString() : err.ToString();
            return new McpDialResult { Ok = false, Message = msg ?? "json-rpc error", Tools = [] };
        }

        if (!root.TryGetProperty("result", out var result))
            return new McpDialResult { Ok = false, Message = "missing result", Tools = [] };

        string? protocol = null;
        string? serverName = null;
        if (result.TryGetProperty("protocolVersion", out var pv))
            protocol = pv.GetString();
        if (result.TryGetProperty("serverInfo", out var si) && si.TryGetProperty("name", out var sn))
            serverName = sn.GetString();

        var tools = new List<McpToolInfoDto>();
        if (result.TryGetProperty("tools", out var toolsEl) && toolsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in toolsEl.EnumerateArray())
            {
                tools.Add(new McpToolInfoDto
                {
                    Name = t.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                    Description = t.TryGetProperty("description", out var d) ? d.GetString() ?? "" : ""
                });
            }
        }

        return new McpDialResult
        {
            Ok = true,
            Message = "ok",
            ProtocolVersion = protocol,
            ServerName = serverName,
            Tools = tools
        };
    }

    private static async Task<(bool Ok, JsonElement? Result, string? ErrorMessage)> PostJsonRpcRawAsync(
        HttpClient client,
        Uri uri,
        object? id,
        string method,
        object? paramsObj,
        string? bearerToken,
        CancellationToken cancellationToken)
    {
        var payload = new { jsonrpc = "2.0", id, method, @params = paramsObj };
        using var req = new HttpRequestMessage(HttpMethod.Post, uri);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(bearerToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

        using var resp = await client.SendAsync(req, cancellationToken);
        var body = await resp.Content.ReadAsStringAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
            return (false, null, $"HTTP {(int)resp.StatusCode}: {Truncate(body, 200)}");

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var root = doc.RootElement.Clone();
        if (root.TryGetProperty("error", out var err))
        {
            var msg = err.TryGetProperty("message", out var em) ? em.GetString() : err.ToString();
            return (false, null, msg ?? "json-rpc error");
        }

        if (!root.TryGetProperty("result", out var result))
            return (false, null, "missing result");

        return (true, result.Clone(), null);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "…";
}

public sealed class McpDialResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ProtocolVersion { get; init; }
    public string? ServerName { get; init; }
    public List<McpToolInfoDto> Tools { get; init; } = [];
}

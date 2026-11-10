using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Agent.Dtos;
using SailNora.Product.Agent.Service;
using SailNora.Product.Chat.Dtos;

namespace SailNora.Product.Agent.Controller;

[ApiController]
[Authorize]
public sealed class AgentChatController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly AgentChatService _chat;

    public AgentChatController(AgentChatService chat) => _chat = chat;

    [HttpPost("api/v1/agent-chat/{sessionId}")]
    [Permission("space:read")]
    public async Task Chat(string sessionId, [FromBody] AgentChatInput input, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        var userId = CurrentUser.GetUserId(User);
        if (spaceId is null || userId is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(ApiResult.Fail("缺少空间/用户上下文", ApiResultCodes.Unauthorized), cancellationToken);
            return;
        }

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var evt in _chat.StreamAsync(spaceId.Value, userId.Value, sessionId, input, cancellationToken))
            {
                var json = JsonSerializer.Serialize(evt, JsonOptions);
                var payload = Encoding.UTF8.GetBytes($"event: message\ndata: {json}\n\n");
                await Response.Body.WriteAsync(payload, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var err = new StreamResponseDto
            {
                ResponseType = "error",
                Done = true,
                Data = new { message = ex.Message }
            };
            var json = JsonSerializer.Serialize(err, JsonOptions);
            var payload = Encoding.UTF8.GetBytes($"event: message\ndata: {json}\n\n");
            await Response.Body.WriteAsync(payload, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}

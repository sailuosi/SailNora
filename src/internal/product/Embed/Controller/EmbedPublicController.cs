using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Models;
using SailNora.Product.Chat.Dtos;
using SailNora.Product.Chat.Service;
using SailNora.Product.Embed.Dtos;
using SailNora.Product.Embed.Service;

namespace SailNora.Product.Embed.Controller;

/// <summary>公开 Embed API：用渠道 token 鉴权（X-Embed-Token / Bearer）。</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/embed/{channelId}")]
public sealed class EmbedPublicController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly EmbedChannelService _channels;
    private readonly SessionService _sessions;
    private readonly KnowledgeChatService _chat;

    public EmbedPublicController(
        EmbedChannelService channels,
        SessionService sessions,
        KnowledgeChatService chat)
    {
        _channels = channels;
        _sessions = sessions;
        _chat = chat;
    }

    [HttpGet("config")]
    [ApiResult]
    public ActionResult<EmbedConfigDto> Config(string channelId)
    {
        var channel = Authenticate(channelId);
        if (channel is null)
            return Unauthorized(ApiResult.Fail("Embed token 无效", ApiResultCodes.Unauthorized));

        return Ok(new EmbedConfigDto
        {
            ChannelId = channel.Id,
            Name = channel.Name,
            KnowledgeBaseIds = EmbedChannelService.ParseKbIds(channel)
        });
    }

    [HttpPost("sessions")]
    [ApiResult]
    public ActionResult<SessionDto> CreateSession(string channelId, [FromBody] CreateEmbedSessionInput? input)
    {
        var channel = Authenticate(channelId);
        if (channel is null)
            return Unauthorized(ApiResult.Fail("Embed token 无效", ApiResultCodes.Unauthorized));

        var guest = EmbedChannelService.GuestUserId(channel.Id);
        var session = _sessions.Create(channel.SpaceId, guest, new CreateSessionInput
        {
            Title = string.IsNullOrWhiteSpace(input?.Title) ? $"embed:{channel.Name}" : input!.Title
        });
        return Ok(session);
    }

    [HttpPost("knowledge-chat/{sessionId}")]
    public async Task KnowledgeChat(
        string channelId,
        string sessionId,
        [FromBody] KnowledgeChatInput input,
        CancellationToken cancellationToken)
    {
        var channel = Authenticate(channelId);
        if (channel is null)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Response.WriteAsJsonAsync(ApiResult.Fail("Embed token 无效", ApiResultCodes.Unauthorized), cancellationToken);
            return;
        }

        var guest = EmbedChannelService.GuestUserId(channel.Id);
        var kbIds = EmbedChannelService.ParseKbIds(channel);
        if ((input.KnowledgeBaseIds is null || input.KnowledgeBaseIds.Count == 0) && kbIds.Count > 0)
            input.KnowledgeBaseIds = kbIds;

        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            await foreach (var evt in _chat.StreamAsync(
                               channel.SpaceId, guest, sessionId, input, cancellationToken))
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

    private DBLayer.KnEmbedChannel? Authenticate(string channelId)
    {
        var token = Request.Headers["X-Embed-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token))
        {
            var auth = Request.Headers.Authorization.ToString();
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = auth["Bearer ".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(token))
            return null;
        return _channels.Authenticate(channelId, token);
    }
}

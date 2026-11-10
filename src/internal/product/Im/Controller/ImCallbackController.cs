using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Product.Im.Dtos;
using SailNora.Product.Im.Service;

namespace SailNora.Product.Im.Controller;

/// <summary>公开 IM 回调：HMAC-SHA256 验签（头 X-IM-Signature / X-Hub-Signature-256）。</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/im/callback/{channelId}")]
public sealed class ImCallbackController : ControllerBase
{
    private readonly ImChannelService _im;

    public ImCallbackController(ImChannelService im) => _im = im;

    [HttpGet]
    public IActionResult Verify(string channelId, [FromQuery] string? challenge)
    {
        var channel = _im.GetEnabled(channelId);
        if (channel is null)
            return NotFound(new { error = "channel not found" });
        if (string.IsNullOrWhiteSpace(challenge))
            return Ok(new { ok = true, channel_id = channelId });
        return Ok(new { challenge });
    }

    [HttpPost]
    public async Task<IActionResult> Callback(string channelId, CancellationToken cancellationToken)
    {
        var channel = _im.GetEnabled(channelId);
        if (channel is null)
            return NotFound(new { error = "channel not found" });

        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-IM-Signature"].FirstOrDefault()
                        ?? Request.Headers["X-Hub-Signature-256"].FirstOrDefault();

        if (!ImChannelService.VerifySignature(channel.Secret, raw, signature))
            return Unauthorized(new { error = "invalid signature" });

        ImWebhookBody body;
        try
        {
            body = JsonSerializer.Deserialize<ImWebhookBody>(raw, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ImWebhookBody();
        }
        catch
        {
            return BadRequest(new { error = "invalid json" });
        }

        try
        {
            var result = _im.HandleInbound(channel, body);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

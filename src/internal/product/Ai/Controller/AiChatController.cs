using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using SailNora.Product.Ai.Dtos;
using SailNora.Product.Ai.Service;

namespace SailNora.Product.Ai.Controller;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiChatController : ControllerBase
{
    private readonly AiChatService _aiChatService;

    public AiChatController(AiChatService aiChatService) => _aiChatService = aiChatService;

    [HttpPost("chat")]
    public async Task Chat([FromBody] AiChatInput input, CancellationToken cancellationToken)
    {
        HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-transform";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var chunk in _aiChatService.StreamChatAsync(input, cancellationToken))
        {
            var payload = Encoding.UTF8.GetBytes($"data: {chunk}\n\n");
            await Response.Body.WriteAsync(payload, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        var done = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
        await Response.Body.WriteAsync(done, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}

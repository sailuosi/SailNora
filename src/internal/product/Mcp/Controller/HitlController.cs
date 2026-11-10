using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Mcp.Dtos;
using SailNora.Product.Mcp.Service;

namespace SailNora.Product.Mcp.Controller;

[ApiController]
[Authorize]
[ApiResult]
public sealed class HitlController : ControllerBase
{
    private readonly HitlService _hitl;

    public HitlController(HitlService hitl) => _hitl = hitl;

    [HttpGet("api/v1/sessions/{sessionId}/tool-approvals")]
    [Permission("space:read")]
    public ActionResult<List<HitlPendingDto>> List(string sessionId, [FromQuery] string? status = null)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_hitl.List(spaceId.Value, sessionId, status));
    }

    [HttpPost("api/v1/sessions/{sessionId}/tool-approvals/{pendingId}/resolve")]
    [Permission("space:read")]
    public ActionResult<HitlPendingDto> Resolve(
        string sessionId,
        string pendingId,
        [FromBody] ResolveHitlInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_hitl.Resolve(spaceId.Value, sessionId, pendingId, input));
    }
}

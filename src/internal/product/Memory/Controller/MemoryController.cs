using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Memory.Dtos;
using SailNora.Product.Memory.Service;

namespace SailNora.Product.Memory.Controller;

[ApiController]
[Authorize]
[ApiResult]
public sealed class MemoryController : ControllerBase
{
    private readonly MemoryService _memory;

    public MemoryController(MemoryService memory) => _memory = memory;

    [HttpGet("api/v1/memories")]
    [Permission("space:read")]
    public ActionResult<List<MemoryDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        var userId = CurrentUser.GetUserId(User);
        if (spaceId is null || userId is null)
            return Unauthorized(ApiResult.Fail("缺少空间/用户上下文", ApiResultCodes.Unauthorized));
        return Ok(_memory.List(spaceId.Value, userId.Value));
    }

    [HttpDelete("api/v1/memories/{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        var userId = CurrentUser.GetUserId(User);
        if (spaceId is null || userId is null)
            return Unauthorized(ApiResult.Fail("缺少空间/用户上下文", ApiResultCodes.Unauthorized));
        _memory.Delete(spaceId.Value, userId.Value, id);
        return Ok(new { deleted = true });
    }

    [HttpPost("api/v1/sessions/{sessionId}/memory/extract")]
    [Permission("space:read")]
    public async Task<ActionResult<MemoryExtractResultDto>> Extract(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        var userId = CurrentUser.GetUserId(User);
        if (spaceId is null || userId is null)
            return Unauthorized(ApiResult.Fail("缺少空间/用户上下文", ApiResultCodes.Unauthorized));
        return Ok(await _memory.ExtractNowAsync(spaceId.Value, userId.Value, sessionId, cancellationToken));
    }
}

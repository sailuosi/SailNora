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
public sealed class McpServiceController : ControllerBase
{
    private readonly McpServiceRegistry _mcp;

    public McpServiceController(McpServiceRegistry mcp) => _mcp = mcp;

    [HttpPost("api/v1/mcp-services")]
    [Permission("space:read")]
    public ActionResult<McpServiceDto> Create([FromBody] CreateMcpServiceInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.CreateService(spaceId.Value, input));
    }

    [HttpGet("api/v1/mcp-services")]
    [Permission("space:read")]
    public ActionResult<List<McpServiceDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.ListServices(spaceId.Value));
    }

    [HttpGet("api/v1/mcp-services/{id}")]
    [Permission("space:read")]
    public ActionResult<McpServiceDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _mcp.GetService(spaceId.Value, id);
        if (dto is null)
            return NotFound(ApiResult.Fail("MCP 服务不存在"));
        return Ok(dto);
    }

    [HttpGet("api/v1/mcp-services/{id}/tools")]
    [Permission("space:read")]
    public async Task<ActionResult<McpToolsListResultDto>> Tools(
        string id,
        [FromQuery] bool refresh = false,
        CancellationToken cancellationToken = default)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var result = await _mcp.ListServiceToolsAsync(spaceId.Value, id, refresh, cancellationToken);
        Response.Headers["X-Mcp-Tools-Cache"] = result.Cached ? "hit" : "miss";
        return Ok(result);
    }

    [HttpPost("api/v1/mcp-services/{id}/tools/invalidate")]
    [Permission("space:read")]
    public ActionResult<object> InvalidateTools(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.InvalidateToolsCache(spaceId.Value, id));
    }

    [HttpPost("api/v1/mcp-services/{id}/test")]
    [Permission("space:read")]
    public async Task<ActionResult<object>> Test(string id, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _mcp.TestServiceAsync(spaceId.Value, id, cancellationToken));
    }

    [HttpPost("api/v1/mcp-services/{id}/tools/call")]
    [Permission("space:read")]
    public async Task<ActionResult<McpCallToolResultDto>> CallTool(
        string id,
        [FromBody] McpCallToolInput input,
        CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _mcp.CallServiceToolAsync(spaceId.Value, id, input, cancellationToken));
    }

    [HttpDelete("api/v1/mcp-services/{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _mcp.DeleteService(spaceId.Value, id);
        return Ok(new { deleted = true });
    }
}

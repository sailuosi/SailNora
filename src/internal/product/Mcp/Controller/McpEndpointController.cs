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
public sealed class McpEndpointController : ControllerBase
{
    private readonly McpServiceRegistry _mcp;

    public McpEndpointController(McpServiceRegistry mcp) => _mcp = mcp;

    [HttpPost("api/v1/mcp-endpoints")]
    [Permission("space:read")]
    public ActionResult<McpEndpointDto> Create([FromBody] CreateMcpEndpointInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.CreateEndpoint(spaceId.Value, input));
    }

    [HttpGet("api/v1/mcp-endpoints")]
    [Permission("space:read")]
    public ActionResult<List<McpEndpointDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.ListEndpoints(spaceId.Value));
    }

    [HttpPost("api/v1/mcp-endpoints/{id}/rotate-token")]
    [Permission("space:read")]
    public ActionResult<McpEndpointDto> Rotate(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.RotateEndpointToken(spaceId.Value, id));
    }

    [HttpDelete("api/v1/mcp-endpoints/{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _mcp.DeleteEndpoint(spaceId.Value, id);
        return Ok(new { deleted = true });
    }
}

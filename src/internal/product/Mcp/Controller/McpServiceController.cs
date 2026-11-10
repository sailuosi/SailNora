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
    public ActionResult<List<McpToolInfoDto>> Tools(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.ListServiceTools(spaceId.Value, id));
    }

    [HttpPost("api/v1/mcp-services/{id}/test")]
    [Permission("space:read")]
    public ActionResult<object> Test(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_mcp.TestService(spaceId.Value, id));
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

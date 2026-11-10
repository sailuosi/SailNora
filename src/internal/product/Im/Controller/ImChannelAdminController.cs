using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Im.Dtos;
using SailNora.Product.Im.Service;

namespace SailNora.Product.Im.Controller;

[ApiController]
[Authorize]
[ApiResult]
[Route("api/v1/im-channels")]
public sealed class ImChannelAdminController : ControllerBase
{
    private readonly ImChannelService _im;

    public ImChannelAdminController(ImChannelService im) => _im = im;

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<ImChannelDto> Create([FromBody] CreateImChannelInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_im.Create(spaceId.Value, input));
    }

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<ImChannelDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_im.List(spaceId.Value));
    }

    [HttpGet("{id}")]
    [Permission("space:read")]
    public ActionResult<ImChannelDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _im.Get(spaceId.Value, id);
        if (dto is null)
            return NotFound(ApiResult.Fail("渠道不存在"));
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _im.Delete(spaceId.Value, id);
        return Ok(new { deleted = true });
    }
}

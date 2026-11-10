using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Embed.Dtos;
using SailNora.Product.Embed.Service;

namespace SailNora.Product.Embed.Controller;

[ApiController]
[Authorize]
[ApiResult]
[Route("api/v1/embed-channels")]
public sealed class EmbedChannelAdminController : ControllerBase
{
    private readonly EmbedChannelService _channels;

    public EmbedChannelAdminController(EmbedChannelService channels) => _channels = channels;

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<EmbedChannelDto> Create([FromBody] CreateEmbedChannelInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_channels.Create(spaceId.Value, input));
    }

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<EmbedChannelDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_channels.List(spaceId.Value));
    }

    [HttpGet("{id}")]
    [Permission("space:read")]
    public ActionResult<EmbedChannelDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _channels.Get(spaceId.Value, id);
        if (dto is null)
            return NotFound(ApiResult.Fail("渠道不存在"));
        return Ok(dto);
    }

    [HttpPost("{id}/rotate-token")]
    [Permission("space:read")]
    public ActionResult<EmbedChannelDto> Rotate(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_channels.RotateToken(spaceId.Value, id));
    }

    [HttpDelete("{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _channels.Delete(spaceId.Value, id);
        return Ok(new { deleted = true });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Platform.Space.DBLayer;
using SailNora.Platform.Space.Dtos;
using SailNora.Platform.Space.Service;

namespace SailNora.Platform.Space.Controller;

[ApiController]
[Route("api/spaces")]
[Authorize]
[ApiResult]
public sealed class SpaceController : ControllerBase
{
    private readonly SpaceService _spaces;

    public SpaceController(SpaceService spaces) => _spaces = spaces;

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<SpaceDto>> ListMine()
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(ApiResult.Fail("未登录", ApiResultCodes.Unauthorized));
        return Ok(_spaces.ListForUser(userId.Value));
    }

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<SpaceDto> Create([FromBody] CreateSpaceInput input)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(ApiResult.Fail("未登录", ApiResultCodes.Unauthorized));
        return Ok(_spaces.Create(userId.Value, input));
    }

    [HttpPost("{spaceId:guid}/api-keys")]
    [Permission("space:read")]
    public ActionResult<CreateApiKeyResult> CreateApiKey(Guid spaceId, [FromBody] CreateApiKeyInput input)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(ApiResult.Fail("未登录", ApiResultCodes.Unauthorized));

        var membership = _spaces.GetMembership(userId.Value, spaceId);
        if (membership is null)
            return StatusCode(StatusCodes.Status403Forbidden, ApiResult.Fail("非空间成员", ApiResultCodes.Forbidden));

        if (membership.Role is not (SpaceRoles.Owner or SpaceRoles.Admin))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResult.Fail("需要 owner/admin", ApiResultCodes.Forbidden));

        return Ok(_spaces.CreateApiKey(spaceId, input));
    }
}


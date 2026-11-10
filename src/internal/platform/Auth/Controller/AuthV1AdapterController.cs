using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Platform.Auth.Dtos;
using SailNora.Platform.Auth.Service;

namespace SailNora.Platform.Auth.Controller;

/// <summary>
/// WeKnora 产品前缀适配：平台仍用 /api/auth/*（admin 形），此处提供 /api/v1/auth/* 入口。
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[ApiResult]
public sealed class AuthV1AdapterController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthV1AdapterController(AuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginInput input, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(input, cancellationToken);
        if (result is null)
            return Unauthorized(ApiResult.Fail("用户名或密码错误", ApiResultCodes.Unauthorized));
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResult>> Me(CancellationToken cancellationToken)
    {
        var userId = CurrentUser.GetUserId(User);
        if (userId is null)
            return Unauthorized(ApiResult.Fail("未登录", ApiResultCodes.Unauthorized));

        var info = await _authService.GetUserInfoAsync(userId.Value, cancellationToken);
        if (info is null)
            return NotFound(ApiResult.Fail("用户不存在"));
        return Ok(info);
    }
}

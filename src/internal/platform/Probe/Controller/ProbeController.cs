using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;

namespace SailNora.Platform.Probe.Controller;

[ApiController]
[Route("api/probe")]
[ApiResult]
public sealed class ProbeController : ControllerBase
{
    [HttpGet("ping")]
    [AllowAnonymous]
    public object Ping() => new { ok = true, service = "sail-nora" };

    [HttpGet("secure")]
    [Authorize]
    [Permission("skeleton:probe")]
    public object Secure() => new { ok = true, account = CurrentUser.GetAccount(User) };
}

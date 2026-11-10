using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Platform.Space.Dtos;

namespace SailNora.Platform.System.Controller;

[ApiController]
[ApiResult]
public sealed class CapabilitiesController : ControllerBase
{
    /// <summary>平台形 capabilities。</summary>
    [HttpGet("api/system/capabilities")]
    [AllowAnonymous]
    public ActionResult<CapabilitiesDto> Get() => Ok(Build());

    /// <summary>WeKnora 产品前缀兼容探测（F1 最小适配）。</summary>
    [HttpGet("api/v1/system/capabilities")]
    [AllowAnonymous]
    public ActionResult<CapabilitiesDto> GetV1() => Ok(Build());

    private static CapabilitiesDto Build() => new()
    {
        Edition = "standard",
        Backend = "dotnet",
        Agent = true,
        Wiki = true,
        Sandbox = false,
        McpServer = true,
        Datasource = true,
        AuthModes = ["jwt", "api_key"]
    };
}

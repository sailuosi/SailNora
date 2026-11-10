using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Sandbox.Dtos;
using SailNora.Product.Sandbox.Service;

namespace SailNora.Product.Sandbox.Controller;

[ApiController]
[Authorize]
[ApiResult]
[Route("api/v1/sandbox-configs")]
public sealed class SandboxConfigController : ControllerBase
{
    private readonly SandboxConfigService _sandboxes;

    public SandboxConfigController(SandboxConfigService sandboxes) => _sandboxes = sandboxes;

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<SandboxConfigDto> Create([FromBody] CreateSandboxConfigInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_sandboxes.Create(spaceId.Value, input));
    }

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<SandboxConfigDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_sandboxes.List(spaceId.Value));
    }

    [HttpGet("{id}")]
    [Permission("space:read")]
    public ActionResult<SandboxConfigDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _sandboxes.Get(spaceId.Value, id);
        if (dto is null)
            return NotFound(ApiResult.Fail("沙箱配置不存在"));
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [Permission("space:read")]
    public ActionResult<SandboxConfigDto> Update(string id, [FromBody] UpdateSandboxConfigInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_sandboxes.Update(spaceId.Value, id, input));
    }

    [HttpDelete("{id}")]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _sandboxes.Delete(spaceId.Value, id);
        return Ok(new { deleted = true });
    }

    [HttpGet("{id}/sandboxes")]
    [Permission("space:read")]
    public ActionResult<SandboxInventoryDto> Inventory(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_sandboxes.Inventory(spaceId.Value, id));
    }

    [HttpPost("{id}/probe")]
    [Permission("space:read")]
    public async Task<ActionResult<SandboxProbeResultDto>> Probe(string id, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _sandboxes.ProbeAsync(spaceId.Value, id, cancellationToken));
    }

    [HttpPost("{id}/exec")]
    [Permission("space:read")]
    public async Task<ActionResult<SandboxCommandResultDto>> Exec(
        string id,
        [FromBody] RunSandboxCommandInput input,
        CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _sandboxes.ExecAsync(spaceId.Value, id, input, cancellationToken));
    }
}

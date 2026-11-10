using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Agent.Dtos;
using SailNora.Product.Agent.Service;

namespace SailNora.Product.Agent.Controller;

[ApiController]
[Authorize]
[ApiResult]
[Route("api/v1/agents")]
public sealed class AgentController : ControllerBase
{
    private readonly AgentService _agents;

    public AgentController(AgentService agents) => _agents = agents;

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<AgentDto> Create([FromBody] CreateAgentInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_agents.Create(spaceId.Value, input));
    }

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<AgentDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_agents.List(spaceId.Value));
    }
}

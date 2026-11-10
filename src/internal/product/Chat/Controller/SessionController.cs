using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Chat.Dtos;
using SailNora.Product.Chat.Service;

namespace SailNora.Product.Chat.Controller;

[ApiController]
[Authorize]
[ApiResult]
[Route("api/v1/sessions")]
public sealed class SessionController : ControllerBase
{
    private readonly SessionService _sessions;

    public SessionController(SessionService sessions) => _sessions = sessions;

    [HttpPost]
    [Permission("space:read")]
    public ActionResult<SessionDto> Create([FromBody] CreateSessionInput? input)
    {
        if (!TrySpaceUser(out var spaceId, out var userId, out var fail))
            return fail!;
        return Ok(_sessions.Create(spaceId, userId, input ?? new CreateSessionInput()));
    }

    [HttpGet]
    [Permission("space:read")]
    public ActionResult<List<SessionDto>> List()
    {
        if (!TrySpaceUser(out var spaceId, out var userId, out var fail))
            return fail!;
        return Ok(_sessions.List(spaceId, userId));
    }

    [HttpGet("{id}")]
    [Permission("space:read")]
    public ActionResult<SessionDto> Get(string id)
    {
        if (!TrySpaceUser(out var spaceId, out var userId, out var fail))
            return fail!;
        var session = _sessions.GetOwned(spaceId, userId, id);
        if (session is null)
            return NotFound(ApiResult.Fail("会话不存在"));
        return Ok(new SessionDto
        {
            Id = session.Id,
            Title = session.Title,
            CreateTime = session.CreateTime,
            UpdateTime = session.UpdateTime
        });
    }

    [HttpGet("{id}/messages")]
    [Permission("space:read")]
    public ActionResult<List<MessageDto>> Messages(string id)
    {
        if (!TrySpaceUser(out var spaceId, out var userId, out var fail))
            return fail!;
        if (_sessions.GetOwned(spaceId, userId, id) is null)
            return NotFound(ApiResult.Fail("会话不存在"));
        return Ok(_sessions.ListMessages(id).Select(m => new MessageDto
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            CreateTime = m.CreateTime
        }).ToList());
    }

    private bool TrySpaceUser(out Guid spaceId, out Guid userId, out ActionResult? fail)
    {
        spaceId = default;
        userId = default;
        fail = null;
        var sid = CurrentUser.GetTenantId(User);
        var uid = CurrentUser.GetUserId(User);
        if (sid is null || uid is null)
        {
            fail = Unauthorized(ApiResult.Fail("缺少空间/用户上下文", ApiResultCodes.Unauthorized));
            return false;
        }

        spaceId = sid.Value;
        userId = uid.Value;
        return true;
    }
}

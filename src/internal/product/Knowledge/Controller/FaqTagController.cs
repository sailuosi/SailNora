using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.Service;

namespace SailNora.Product.Knowledge.Controller;

[ApiController]
[Authorize]
[ApiResult]
public sealed class FaqTagController : ControllerBase
{
    private readonly TagService _tags;
    private readonly FaqService _faq;

    public FaqTagController(TagService tags, FaqService faq)
    {
        _tags = tags;
        _faq = faq;
    }

    [HttpPost("api/v1/knowledge-bases/{id}/tags")]
    [Permission("space:read")]
    public ActionResult<KnowledgeTagDto> CreateTag(string id, [FromBody] CreateTagInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_tags.Create(spaceId.Value, id, input));
    }

    [HttpGet("api/v1/knowledge-bases/{id}/tags")]
    [Permission("space:read")]
    public ActionResult<List<KnowledgeTagDto>> ListTags(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_tags.List(spaceId.Value, id));
    }

    [HttpDelete("api/v1/knowledge-bases/{id}/tags/{tagId}")]
    [Permission("space:read")]
    public ActionResult DeleteTag(string id, string tagId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _tags.Delete(spaceId.Value, id, tagId);
        return Ok(new { deleted = true });
    }

    [HttpPost("api/v1/knowledge-bases/{id}/faq/entries")]
    [Permission("space:read")]
    public ActionResult<FaqEntryDto> CreateFaq(string id, [FromBody] CreateFaqEntryInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_faq.Create(spaceId.Value, id, input));
    }

    [HttpGet("api/v1/knowledge-bases/{id}/faq/entries")]
    [Permission("space:read")]
    public ActionResult<List<FaqEntryDto>> ListFaq(string id, [FromQuery] bool? enabled = null)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_faq.List(spaceId.Value, id, enabled));
    }

    [HttpGet("api/v1/knowledge-bases/{id}/faq/entries/{entryId}")]
    [Permission("space:read")]
    public ActionResult<FaqEntryDto> GetFaq(string id, string entryId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _faq.Get(spaceId.Value, id, entryId);
        if (dto is null)
            return NotFound(ApiResult.Fail("FAQ 不存在"));
        return Ok(dto);
    }

    [HttpPut("api/v1/knowledge-bases/{id}/faq/entries/{entryId}")]
    [Permission("space:read")]
    public ActionResult<FaqEntryDto> UpdateFaq(string id, string entryId, [FromBody] UpdateFaqEntryInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_faq.Update(spaceId.Value, id, entryId, input));
    }

    [HttpDelete("api/v1/knowledge-bases/{id}/faq/entries/{entryId}")]
    [Permission("space:read")]
    public ActionResult DeleteFaq(string id, string entryId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _faq.Delete(spaceId.Value, id, entryId);
        return Ok(new { deleted = true });
    }
}

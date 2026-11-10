using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Wiki.Dtos;
using SailNora.Product.Wiki.Jobs;
using SailNora.Product.Wiki.Service;

namespace SailNora.Product.Wiki.Controller;

[ApiController]
[Authorize]
[ApiResult]
public sealed class WikiController : ControllerBase
{
    private readonly WikiService _wiki;
    private readonly WikiIngestJob _ingest;

    public WikiController(WikiService wiki, WikiIngestJob ingest)
    {
        _wiki = wiki;
        _ingest = ingest;
    }

    [HttpPost("api/v1/wiki/ingest")]
    [Permission("space:read")]
    public async Task<ActionResult<WikiIngestResultDto>> Ingest(
        [FromBody] WikiIngestRequest input,
        CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        if (string.IsNullOrWhiteSpace(input.KnowledgeId))
            return BadRequest(ApiResult.Fail("缺少 knowledgeId"));

        var result = await _ingest.ExecuteAsync(new WikiIngestPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId.Value,
            KnowledgeId = input.KnowledgeId.Trim()
        }, cancellationToken);
        if (result is null)
            return NotFound(ApiResult.Fail("知识不存在"));
        return Ok(result);
    }

    [HttpPost("api/v1/wiki/pages")]
    [Permission("space:read")]
    public ActionResult<WikiPageDto> Create([FromBody] CreateWikiPageInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_wiki.Create(spaceId.Value, input));
    }

    [HttpGet("api/v1/wiki/pages")]
    [Permission("space:read")]
    public ActionResult<List<WikiPageSummaryDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_wiki.List(spaceId.Value));
    }

    [HttpGet("api/v1/wiki/pages/by-slug/{slug}")]
    [Permission("space:read")]
    public ActionResult<WikiPageDto> GetBySlug(string slug)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var page = _wiki.GetBySlug(spaceId.Value, slug);
        if (page is null)
            return NotFound(ApiResult.Fail("页面不存在"));
        return Ok(page);
    }

    [HttpGet("api/v1/wiki/pages/{id}")]
    [Permission("space:read")]
    public ActionResult<WikiPageDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var page = _wiki.GetById(spaceId.Value, id);
        if (page is null)
            return NotFound(ApiResult.Fail("页面不存在"));
        return Ok(page);
    }

    [HttpPut("api/v1/wiki/pages/{id}")]
    [Permission("space:read")]
    public ActionResult<WikiPageDto> Update(string id, [FromBody] UpdateWikiPageInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_wiki.Update(spaceId.Value, id, input));
    }
}

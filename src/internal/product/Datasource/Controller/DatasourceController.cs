using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailNora.Foundation.AspNet;
using SailNora.Foundation.Auth;
using SailNora.Foundation.Models;
using SailNora.Product.Datasource.Dtos;
using SailNora.Product.Datasource.Service;

namespace SailNora.Product.Datasource.Controller;

[ApiController]
[ApiResult]
public sealed class DatasourceController : ControllerBase
{
    private readonly DatasourceService _datasources;

    public DatasourceController(DatasourceService datasources) => _datasources = datasources;

    /// <summary>本地冒烟用样例 RSS（无需外网）。</summary>
    [HttpGet("api/v1/datasources/sample-feed")]
    [AllowAnonymous]
    public ContentResult SampleFeed()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>sail-nora sample</title>
                <item>
                  <title>RSS 退款说明</title>
                  <link>https://example.com/refund</link>
                  <guid>sample-refund-1</guid>
                  <description>购买后 14 天内可退款（RSS 样例）。</description>
                </item>
                <item>
                  <title>RSS 安装指南</title>
                  <link>https://example.com/install</link>
                  <guid>sample-install-1</guid>
                  <description>先检查依赖再安装（RSS 样例）。</description>
                </item>
              </channel>
            </rss>
            """;
        return Content(xml, "application/rss+xml; charset=utf-8");
    }

    /// <summary>本地冒烟用样例 GitLab tree（无需外网/PAT）。</summary>
    [HttpGet("api/v1/datasources/sample-gitlab-tree")]
    [AllowAnonymous]
    public ContentResult SampleGitlabTree()
    {
        const string json = """
            {
              "tree": [
                {
                  "id": "blob-refund-1",
                  "path": "docs/refund.md",
                  "type": "blob",
                  "content": "GitLab 样例：购买后 14 天内可退款。"
                },
                {
                  "id": "blob-install-1",
                  "path": "docs/install.md",
                  "type": "blob",
                  "content": "GitLab 样例：先检查依赖再安装。"
                }
              ]
            }
            """;
        return Content(json, "application/json; charset=utf-8");
    }

    /// <summary>本地冒烟用样例 Confluence pages（无需外网）。</summary>
    [HttpGet("api/v1/datasources/sample-confluence-pages")]
    [AllowAnonymous]
    public ContentResult SampleConfluencePages()
    {
        const string json = """
            {
              "pages": [
                {
                  "id": "cf-refund-1",
                  "title": "Confluence 退款说明",
                  "body": "Confluence 样例：购买后 14 天内可退款。"
                },
                {
                  "id": "cf-install-1",
                  "title": "Confluence 安装指南",
                  "body": "Confluence 样例：先检查依赖再安装。"
                }
              ]
            }
            """;
        return Content(json, "application/json; charset=utf-8");
    }

    /// <summary>本地冒烟用样例飞书文档（无需外网/租户）。</summary>
    [HttpGet("api/v1/datasources/sample-feishu-docs")]
    [AllowAnonymous]
    public ContentResult SampleFeishuDocs()
    {
        const string json = """
            {
              "docs": [
                {
                  "id": "fs-refund-1",
                  "title": "飞书 退款说明",
                  "body": "飞书样例：购买后 14 天内可退款。"
                },
                {
                  "id": "fs-install-1",
                  "title": "飞书 安装指南",
                  "body": "飞书样例：先检查依赖再安装。"
                }
              ]
            }
            """;
        return Content(json, "application/json; charset=utf-8");
    }

    [HttpPost("api/v1/datasources")]
    [Authorize]
    [Permission("space:read")]
    public ActionResult<DatasourceDto> Create([FromBody] CreateDatasourceInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_datasources.Create(spaceId.Value, input));
    }

    [HttpGet("api/v1/datasources")]
    [Authorize]
    [Permission("space:read")]
    public ActionResult<List<DatasourceDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_datasources.List(spaceId.Value));
    }

    [HttpGet("api/v1/datasources/{id}")]
    [Authorize]
    [Permission("space:read")]
    public ActionResult<DatasourceDto> Get(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _datasources.Get(spaceId.Value, id);
        if (dto is null)
            return NotFound(ApiResult.Fail("数据源不存在"));
        return Ok(dto);
    }

    [HttpDelete("api/v1/datasources/{id}")]
    [Authorize]
    [Permission("space:read")]
    public ActionResult Delete(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _datasources.Delete(spaceId.Value, id);
        return Ok(new { deleted = true });
    }

    [HttpPost("api/v1/datasources/{id}/sync")]
    [Authorize]
    [Permission("space:read")]
    public async Task<ActionResult<DatasourceSyncResultDto>> Sync(string id, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _datasources.SyncNowAsync(spaceId.Value, id, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
public sealed class KnowledgeBaseController : ControllerBase
{
    private readonly KnowledgeService _knowledge;

    public KnowledgeBaseController(KnowledgeService knowledge) => _knowledge = knowledge;

    [HttpPost("api/v1/knowledge-bases")]
    [Permission("space:read")]
    public ActionResult<KnowledgeBaseDto> Create([FromBody] CreateKnowledgeBaseInput input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.CreateBase(spaceId.Value, input));
    }

    [HttpGet("api/v1/knowledge-bases")]
    [Permission("space:read")]
    public ActionResult<List<KnowledgeBaseDto>> List()
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.ListBases(spaceId.Value));
    }

    [HttpPost("api/v1/knowledge-bases/{id}/knowledge/file")]
    [Permission("space:read")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<KnowledgeDto>> Upload(string id, IFormFile file, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        if (file is null || file.Length == 0)
            return BadRequest(ApiResult.Fail("缺少文件"));

        await using var stream = file.OpenReadStream();
        var dto = await _knowledge.UploadFileAsync(
            spaceId.Value,
            id,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);
        return Ok(dto);
    }

    [HttpGet("api/v1/knowledge-bases/{id}/knowledge")]
    [Permission("space:read")]
    public ActionResult<List<KnowledgeDto>> ListKnowledge(string id)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.ListKnowledge(spaceId.Value, id));
    }

    [HttpGet("api/v1/chunks/{knowledgeId}")]
    [Permission("space:read")]
    public ActionResult<List<ChunkDto>> ListChunks(string knowledgeId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.ListChunks(spaceId.Value, knowledgeId));
    }

    [HttpGet("api/v1/chunks/by-id/{chunkId}")]
    [Permission("space:read")]
    public ActionResult<ChunkDto> GetChunk(string chunkId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _knowledge.GetChunk(spaceId.Value, chunkId);
        if (dto is null)
            return NotFound(ApiResult.Fail("分块不存在"));
        return Ok(dto);
    }

    [HttpPut("api/v1/chunks/{knowledgeId}/{chunkId}")]
    [Permission("space:read")]
    public async Task<ActionResult<ChunkDto>> UpdateChunk(
        string knowledgeId,
        string chunkId,
        [FromBody] UpdateChunkInput input,
        CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(await _knowledge.UpdateChunkAsync(spaceId.Value, knowledgeId, chunkId, input, cancellationToken));
    }

    [HttpGet("api/v1/chunks/{knowledgeId}/{chunkId}/revisions")]
    [Permission("space:read")]
    public ActionResult<List<ChunkRevisionDto>> ListChunkRevisions(string knowledgeId, string chunkId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.ListChunkRevisions(spaceId.Value, knowledgeId, chunkId));
    }

    [HttpPost("api/v1/chunks/{knowledgeId}/{chunkId}/revert")]
    [Permission("space:read")]
    public ActionResult<ChunkDto> RevertChunk(
        string knowledgeId,
        string chunkId,
        [FromBody] RevertChunkInput? input)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        return Ok(_knowledge.RevertChunk(spaceId.Value, knowledgeId, chunkId, input ?? new RevertChunkInput()));
    }

    [HttpDelete("api/v1/chunks/{knowledgeId}/{chunkId}")]
    [Permission("space:read")]
    public ActionResult DeleteChunk(string knowledgeId, string chunkId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        _knowledge.DeleteChunk(spaceId.Value, knowledgeId, chunkId);
        return Ok(new { deleted = true });
    }

    [HttpGet("api/v1/knowledge/{knowledgeId}")]
    [Permission("space:read")]
    public ActionResult<KnowledgeDto> GetKnowledge(string knowledgeId)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        var dto = _knowledge.GetKnowledge(spaceId.Value, knowledgeId);
        if (dto is null)
            return NotFound(ApiResult.Fail("知识不存在"));
        return Ok(dto);
    }
}

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
public sealed class KnowledgeSearchController : ControllerBase
{
    private readonly ChunkRetrievalService _retrieval;

    public KnowledgeSearchController(ChunkRetrievalService retrieval) => _retrieval = retrieval;

    [HttpPost("api/v1/knowledge-search")]
    [Permission("space:read")]
    public async Task<ActionResult<object>> Search([FromBody] KnowledgeSearchInput input, CancellationToken cancellationToken)
    {
        var spaceId = CurrentUser.GetTenantId(User);
        if (spaceId is null)
            return Unauthorized(ApiResult.Fail("缺少空间上下文", ApiResultCodes.Unauthorized));
        if (string.IsNullOrWhiteSpace(input.Query))
            return BadRequest(ApiResult.Fail("query 不能为空"));

        var hits = await _retrieval.SearchAsync(
            spaceId.Value,
            input.Query,
            input.KnowledgeBaseIds,
            input.KnowledgeIds,
            input.MatchCount ?? 10,
            cancellationToken);

        return Ok(new
        {
            items = hits.Select(h => new
            {
                chunk_id = h.Id,
                knowledge_id = h.KnowledgeId,
                knowledge_base_id = h.KnowledgeBaseId,
                content = h.Content,
                score = Math.Round(h.Score, 4),
                keyword_score = Math.Round(h.KeywordScore, 4),
                vector_score = Math.Round(h.VectorScore, 4)
            }).ToList(),
            total = hits.Count
        });
    }
}

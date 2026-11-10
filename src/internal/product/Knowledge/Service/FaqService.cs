using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;

namespace SailNora.Product.Knowledge.Service;

[Scoped]
public sealed class FaqService
{
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly MooRepository<KnFaqEntry> _entries;
    private readonly MooRepository<KnKnowledgeTag> _tags;

    public FaqService(
        MooRepository<KnKnowledgeBase> bases,
        MooRepository<KnFaqEntry> entries,
        MooRepository<KnKnowledgeTag> tags)
    {
        _bases = bases;
        _entries = entries;
        _tags = tags;
    }

    public FaqEntryDto Create(Guid spaceId, string knowledgeBaseId, CreateFaqEntryInput input)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        if (string.IsNullOrWhiteSpace(input.Question))
            throw new InvalidOperationException("问题不能为空");
        if (string.IsNullOrWhiteSpace(input.Answer))
            throw new InvalidOperationException("答案不能为空");
        ValidateTag(spaceId, knowledgeBaseId, input.TagId);

        var now = DateTime.Now;
        var entity = new KnFaqEntry
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            KnowledgeBaseId = knowledgeBaseId,
            Question = input.Question.Trim(),
            Answer = input.Answer,
            TagId = string.IsNullOrWhiteSpace(input.TagId) ? null : input.TagId.Trim(),
            Enabled = input.Enabled,
            CreateTime = now,
            UpdateTime = now
        };
        _entries.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<FaqEntryDto> List(Guid spaceId, string knowledgeBaseId, bool? enabled = null)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        var list = _entries.Repo.GetList(e => e.SpaceId == spaceId && e.KnowledgeBaseId == knowledgeBaseId);
        if (enabled is not null)
            list = list.Where(e => e.Enabled == enabled.Value).ToList();
        return list.OrderByDescending(e => e.UpdateTime).Select(ToDto).ToList();
    }

    public FaqEntryDto? Get(Guid spaceId, string knowledgeBaseId, string entryId)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        var entry = _entries.Repo.GetById(entryId);
        if (entry is null || entry.SpaceId != spaceId || entry.KnowledgeBaseId != knowledgeBaseId)
            return null;
        return ToDto(entry);
    }

    public FaqEntryDto Update(Guid spaceId, string knowledgeBaseId, string entryId, UpdateFaqEntryInput input)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        var entry = _entries.Repo.GetById(entryId)
                    ?? throw new InvalidOperationException("FAQ 不存在");
        if (entry.SpaceId != spaceId || entry.KnowledgeBaseId != knowledgeBaseId)
            throw new UnauthorizedAccessException("无权访问");

        if (!string.IsNullOrWhiteSpace(input.Question))
            entry.Question = input.Question.Trim();
        if (input.Answer is not null)
            entry.Answer = input.Answer;
        if (input.TagId is not null)
        {
            var tagId = string.IsNullOrWhiteSpace(input.TagId) ? null : input.TagId.Trim();
            ValidateTag(spaceId, knowledgeBaseId, tagId);
            entry.TagId = tagId;
        }
        if (input.Enabled is not null)
            entry.Enabled = input.Enabled.Value;
        entry.UpdateTime = DateTime.Now;
        _entries.Repo.Update(entry);
        return ToDto(entry);
    }

    public void Delete(Guid spaceId, string knowledgeBaseId, string entryId)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        var entry = _entries.Repo.GetById(entryId)
                    ?? throw new InvalidOperationException("FAQ 不存在");
        if (entry.SpaceId != spaceId || entry.KnowledgeBaseId != knowledgeBaseId)
            throw new UnauthorizedAccessException("无权访问");
        _entries.Repo.Delete(entry);
    }

    private void EnsureBase(Guid spaceId, string knowledgeBaseId)
    {
        var kb = _bases.Repo.GetById(knowledgeBaseId)
                 ?? throw new InvalidOperationException("知识库不存在");
        if (kb.SpaceId != spaceId)
            throw new UnauthorizedAccessException("知识库不属于当前空间");
    }

    private void ValidateTag(Guid spaceId, string knowledgeBaseId, string? tagId)
    {
        if (string.IsNullOrWhiteSpace(tagId))
            return;
        var tag = _tags.Repo.GetById(tagId)
                  ?? throw new InvalidOperationException("标签不存在");
        if (tag.SpaceId != spaceId || tag.KnowledgeBaseId != knowledgeBaseId)
            throw new InvalidOperationException("标签不属于该知识库");
    }

    private static FaqEntryDto ToDto(KnFaqEntry e) => new()
    {
        Id = e.Id,
        KnowledgeBaseId = e.KnowledgeBaseId,
        Question = e.Question,
        Answer = e.Answer,
        TagId = e.TagId,
        Enabled = e.Enabled,
        CreateTime = e.CreateTime,
        UpdateTime = e.UpdateTime
    };
}

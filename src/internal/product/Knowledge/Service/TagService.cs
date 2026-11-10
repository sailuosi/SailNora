using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;

namespace SailNora.Product.Knowledge.Service;

[Scoped]
public sealed class TagService
{
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly MooRepository<KnKnowledgeTag> _tags;

    public TagService(MooRepository<KnKnowledgeBase> bases, MooRepository<KnKnowledgeTag> tags)
    {
        _bases = bases;
        _tags = tags;
    }

    public KnowledgeTagDto Create(Guid spaceId, string knowledgeBaseId, CreateTagInput input)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("标签名不能为空");

        var name = input.Name.Trim();
        if (_tags.Repo.GetFirst(t => t.KnowledgeBaseId == knowledgeBaseId && t.Name == name) is not null)
            throw new InvalidOperationException("标签已存在");

        var entity = new KnKnowledgeTag
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            KnowledgeBaseId = knowledgeBaseId,
            Name = name,
            CreateTime = DateTime.Now
        };
        _tags.Repo.Insert(entity);
        return ToDto(entity);
    }

    public List<KnowledgeTagDto> List(Guid spaceId, string knowledgeBaseId)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        return _tags.Repo.GetList(t => t.SpaceId == spaceId && t.KnowledgeBaseId == knowledgeBaseId)
            .OrderBy(t => t.Name)
            .Select(ToDto)
            .ToList();
    }

    public void Delete(Guid spaceId, string knowledgeBaseId, string tagId)
    {
        EnsureBase(spaceId, knowledgeBaseId);
        var tag = _tags.Repo.GetById(tagId)
                  ?? throw new InvalidOperationException("标签不存在");
        if (tag.SpaceId != spaceId || tag.KnowledgeBaseId != knowledgeBaseId)
            throw new UnauthorizedAccessException("无权访问");
        _tags.Repo.Delete(tag);
    }

    private void EnsureBase(Guid spaceId, string knowledgeBaseId)
    {
        var kb = _bases.Repo.GetById(knowledgeBaseId)
                 ?? throw new InvalidOperationException("知识库不存在");
        if (kb.SpaceId != spaceId)
            throw new UnauthorizedAccessException("知识库不属于当前空间");
    }

    private static KnowledgeTagDto ToDto(KnKnowledgeTag t) => new()
    {
        Id = t.Id,
        KnowledgeBaseId = t.KnowledgeBaseId,
        Name = t.Name,
        CreateTime = t.CreateTime
    };
}

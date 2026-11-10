using System.Security.Cryptography;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Storage;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.Product.Knowledge.Service;

public sealed class KnowledgeService : IScoped
{
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly MooRepository<KnChunk> _chunks;
    private readonly ILocalFileStorage _storage;
    private readonly IDocumentProcessEnqueue _enqueue;

    public KnowledgeService(
        MooRepository<KnKnowledgeBase> bases,
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks,
        ILocalFileStorage storage,
        IDocumentProcessEnqueue enqueue)
    {
        _bases = bases;
        _knowledges = knowledges;
        _chunks = chunks;
        _storage = storage;
        _enqueue = enqueue;
    }

    public KnowledgeBaseDto CreateBase(Guid spaceId, CreateKnowledgeBaseInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new InvalidOperationException("知识库名称不能为空");

        var entity = new KnKnowledgeBase
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = spaceId,
            Name = input.Name.Trim(),
            Description = input.Description?.Trim(),
            Type = string.IsNullOrWhiteSpace(input.Type) ? KnowledgeBaseTypes.Document : input.Type.Trim(),
            CreateTime = DateTime.Now
        };
        _bases.Repo.Insert(entity);
        return ToBaseDto(entity);
    }

    public List<KnowledgeBaseDto> ListBases(Guid spaceId) =>
        _bases.Repo.GetList(b => b.SpaceId == spaceId)
            .OrderByDescending(b => b.CreateTime)
            .Select(ToBaseDto)
            .ToList();

    public async Task<KnowledgeDto> UploadFileAsync(
        Guid spaceId,
        string knowledgeBaseId,
        Stream content,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        var kb = _bases.Repo.GetById(knowledgeBaseId)
                 ?? throw new InvalidOperationException("知识库不存在");
        if (kb.SpaceId != spaceId)
            throw new UnauthorizedAccessException("知识库不属于当前空间");
        if (kb.Type is KnowledgeBaseTypes.Faq)
            throw new InvalidOperationException("FAQ 知识库不支持文件上传");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(buffer.ToArray())).ToLowerInvariant();
        buffer.Position = 0;

        var dup = _knowledges.Repo.GetFirst(k =>
            k.KnowledgeBaseId == knowledgeBaseId && k.FileHash == hash);
        if (dup is not null)
            throw new InvalidOperationException("文件已存在（hash 重复）");

        var knowledgeId = Guid.NewGuid().ToString();
        var ext = Path.GetExtension(fileName);
        var relative = $"{spaceId:N}/{knowledgeId}/{SanitizeFileName(fileName)}";
        await _storage.SaveAsync(buffer, relative, cancellationToken);

        var entity = new KnKnowledge
        {
            Id = knowledgeId,
            SpaceId = spaceId,
            KnowledgeBaseId = knowledgeBaseId,
            Type = "file",
            Title = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName,
            FileType = string.IsNullOrWhiteSpace(contentType) ? ext.TrimStart('.') : contentType,
            FileSize = buffer.Length,
            FilePath = relative,
            FileHash = hash,
            ParseStatus = ParseStatuses.Pending,
            CreateTime = DateTime.Now
        };
        _knowledges.Repo.Insert(entity);

        _enqueue.Enqueue(new DocumentProcessPayload
        {
            RequestId = Guid.NewGuid().ToString("N"),
            SpaceId = spaceId,
            KnowledgeId = knowledgeId,
            KnowledgeBaseId = knowledgeBaseId,
            FilePath = relative,
            FileName = fileName,
            FileType = entity.FileType
        });

        // 同步/异步处理后重新读取状态
        var fresh = _knowledges.Repo.GetById(knowledgeId) ?? entity;
        return ToKnowledgeDto(fresh);
    }

    public KnowledgeDto? GetKnowledge(Guid spaceId, string knowledgeId)
    {
        var entity = _knowledges.Repo.GetById(knowledgeId);
        if (entity is null || entity.SpaceId != spaceId)
            return null;
        return ToKnowledgeDto(entity);
    }

    public List<ChunkDto> ListChunks(Guid spaceId, string knowledgeId)
    {
        var knowledge = _knowledges.Repo.GetById(knowledgeId);
        if (knowledge is null || knowledge.SpaceId != spaceId)
            return [];

        return _chunks.Repo.GetList(c => c.KnowledgeId == knowledgeId)
            .OrderBy(c => c.ChunkIndex)
            .Select(c => new ChunkDto
            {
                Id = c.Id,
                KnowledgeId = c.KnowledgeId,
                Content = c.Content,
                ChunkIndex = c.ChunkIndex,
                ChunkType = c.ChunkType
            })
            .ToList();
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "file.bin" : name;
    }

    private static KnowledgeBaseDto ToBaseDto(KnKnowledgeBase e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Type = e.Type,
        SpaceId = e.SpaceId.ToString(),
        CreateTime = e.CreateTime
    };

    private static KnowledgeDto ToKnowledgeDto(KnKnowledge e) => new()
    {
        Id = e.Id,
        KnowledgeBaseId = e.KnowledgeBaseId,
        Title = e.Title,
        FileName = e.FileName,
        FileType = e.FileType,
        FileSize = e.FileSize,
        ParseStatus = e.ParseStatus,
        ErrorMessage = e.ErrorMessage,
        CreateTime = e.CreateTime
    };
}

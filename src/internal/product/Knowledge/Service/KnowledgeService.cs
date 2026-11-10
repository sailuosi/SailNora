using System.Security.Cryptography;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Storage;
using SailNora.Product.Ai.Embedding;
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
    private readonly ITextEmbedder _embedder;

    public KnowledgeService(
        MooRepository<KnKnowledgeBase> bases,
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks,
        ILocalFileStorage storage,
        IDocumentProcessEnqueue enqueue,
        ITextEmbedder embedder)
    {
        _bases = bases;
        _knowledges = knowledges;
        _chunks = chunks;
        _storage = storage;
        _enqueue = enqueue;
        _embedder = embedder;
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

    public KnowledgeBaseDto? GetBase(Guid spaceId, string knowledgeBaseId)
    {
        var kb = _bases.Repo.GetById(knowledgeBaseId);
        if (kb is null || kb.SpaceId != spaceId)
            return null;
        return ToBaseDto(kb);
    }

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

    public List<KnowledgeDto> ListKnowledge(Guid spaceId, string knowledgeBaseId)
    {
        var kb = _bases.Repo.GetById(knowledgeBaseId);
        if (kb is null || kb.SpaceId != spaceId)
            return [];

        return _knowledges.Repo.GetList(k => k.KnowledgeBaseId == knowledgeBaseId && k.SpaceId == spaceId)
            .OrderByDescending(k => k.CreateTime)
            .Select(ToKnowledgeDto)
            .ToList();
    }

    public List<ChunkDto> ListChunks(Guid spaceId, string knowledgeId)
    {
        var knowledge = _knowledges.Repo.GetById(knowledgeId);
        if (knowledge is null || knowledge.SpaceId != spaceId)
            return [];

        return _chunks.Repo.GetList(c => c.KnowledgeId == knowledgeId)
            .OrderBy(c => c.ChunkIndex)
            .Select(ToChunkDto)
            .ToList();
    }

    public ChunkDto? GetChunk(Guid spaceId, string chunkId)
    {
        var chunk = _chunks.Repo.GetById(chunkId);
        if (chunk is null || chunk.SpaceId != spaceId)
            return null;
        return ToChunkDto(chunk);
    }

    public async Task<ChunkDto> UpdateChunkAsync(
        Guid spaceId,
        string knowledgeId,
        string chunkId,
        UpdateChunkInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Content))
            throw new InvalidOperationException("分块内容不能为空");

        var knowledge = _knowledges.Repo.GetById(knowledgeId)
                        ?? throw new InvalidOperationException("知识不存在");
        if (knowledge.SpaceId != spaceId)
            throw new UnauthorizedAccessException("无权访问");

        var chunk = _chunks.Repo.GetById(chunkId)
                    ?? throw new InvalidOperationException("分块不存在");
        if (chunk.SpaceId != spaceId || chunk.KnowledgeId != knowledgeId)
            throw new InvalidOperationException("分块与知识不匹配");

        chunk.Content = input.Content.Trim();
        var vector = await _embedder.EmbedAsync(chunk.Content, cancellationToken);
        chunk.EmbeddingJson = EmbeddingCodec.ToJson(vector);
        _chunks.Repo.Update(chunk);
        return ToChunkDto(chunk);
    }

    public void DeleteChunk(Guid spaceId, string knowledgeId, string chunkId)
    {
        var chunk = _chunks.Repo.GetById(chunkId)
                    ?? throw new InvalidOperationException("分块不存在");
        if (chunk.SpaceId != spaceId || chunk.KnowledgeId != knowledgeId)
            throw new InvalidOperationException("分块与知识不匹配");
        _chunks.Repo.Delete(chunk);
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

    private static ChunkDto ToChunkDto(KnChunk c) => new()
    {
        Id = c.Id,
        KnowledgeId = c.KnowledgeId,
        KnowledgeBaseId = c.KnowledgeBaseId,
        Content = c.Content,
        ChunkIndex = c.ChunkIndex,
        ChunkType = c.ChunkType,
        HasEmbedding = !string.IsNullOrWhiteSpace(c.EmbeddingJson)
    };
}

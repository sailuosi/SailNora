using System.Security.Cryptography;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Storage;
using SailNora.Product.Ai.Embedding;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.Jobs;

namespace SailNora.Product.Knowledge.Service;

[Scoped]
public sealed class KnowledgeService
{
    private readonly MooRepository<KnKnowledgeBase> _bases;
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly MooRepository<KnChunk> _chunks;
    private readonly MooRepository<KnChunkRevision> _revisions;
    private readonly ILocalFileStorage _storage;
    private readonly IDocumentProcessEnqueue _enqueue;
    private readonly ITextEmbedder _embedder;

    public KnowledgeService(
        MooRepository<KnKnowledgeBase> bases,
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks,
        MooRepository<KnChunkRevision> revisions,
        ILocalFileStorage storage,
        IDocumentProcessEnqueue enqueue,
        ITextEmbedder embedder)
    {
        _bases = bases;
        _knowledges = knowledges;
        _chunks = chunks;
        _revisions = revisions;
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

        SnapshotRevision(chunk);

        chunk.Content = input.Content.Trim();
        var vector = await _embedder.EmbedAsync(chunk.Content, cancellationToken);
        chunk.EmbeddingJson = EmbeddingCodec.ToJson(vector);
        chunk.ContentRevision += 1;
        _chunks.Repo.Update(chunk);
        return ToChunkDto(chunk);
    }

    public List<ChunkRevisionDto> ListChunkRevisions(Guid spaceId, string knowledgeId, string chunkId)
    {
        var chunk = RequireOwnedChunk(spaceId, knowledgeId, chunkId);
        return _revisions.Repo.GetList(r => r.ChunkId == chunk.Id && r.SpaceId == spaceId)
            .OrderByDescending(r => r.Revision)
            .Select(r => new ChunkRevisionDto
            {
                Revision = r.Revision,
                Content = r.Content,
                HasEmbedding = !string.IsNullOrWhiteSpace(r.EmbeddingJson),
                EditedAt = r.EditedAt
            })
            .ToList();
    }

    public ChunkDto RevertChunk(Guid spaceId, string knowledgeId, string chunkId, RevertChunkInput input)
    {
        var chunk = RequireOwnedChunk(spaceId, knowledgeId, chunkId);
        if (input.ExpectedRevision is int expected && chunk.ContentRevision != expected)
            throw new InvalidOperationException(
                $"修订冲突：期望 {expected}，当前 {chunk.ContentRevision}");

        KnChunkRevision? hist;
        if (input.Revision is int target)
        {
            hist = _revisions.Repo.GetFirst(r =>
                r.ChunkId == chunk.Id && r.SpaceId == spaceId && r.Revision == target);
        }
        else
        {
            hist = _revisions.Repo.GetList(r => r.ChunkId == chunk.Id && r.SpaceId == spaceId)
                .OrderByDescending(r => r.Revision)
                .FirstOrDefault();
        }

        if (hist is null)
            throw new InvalidOperationException("无可回滚的历史修订");

        SnapshotRevision(chunk);
        chunk.Content = hist.Content;
        chunk.EmbeddingJson = hist.EmbeddingJson;
        chunk.ContentRevision += 1;
        _chunks.Repo.Update(chunk);
        return ToChunkDto(chunk);
    }

    public void DeleteChunk(Guid spaceId, string knowledgeId, string chunkId)
    {
        var chunk = RequireOwnedChunk(spaceId, knowledgeId, chunkId);
        var revs = _revisions.Repo.GetList(r => r.ChunkId == chunk.Id);
        foreach (var r in revs)
            _revisions.Repo.Delete(r);
        _chunks.Repo.Delete(chunk);
    }

    private KnChunk RequireOwnedChunk(Guid spaceId, string knowledgeId, string chunkId)
    {
        var chunk = _chunks.Repo.GetById(chunkId)
                    ?? throw new InvalidOperationException("分块不存在");
        if (chunk.SpaceId != spaceId || chunk.KnowledgeId != knowledgeId)
            throw new InvalidOperationException("分块与知识不匹配");
        return chunk;
    }

    private void SnapshotRevision(KnChunk chunk)
    {
        _revisions.Repo.Insert(new KnChunkRevision
        {
            Id = Guid.NewGuid().ToString(),
            SpaceId = chunk.SpaceId,
            ChunkId = chunk.Id,
            KnowledgeId = chunk.KnowledgeId,
            Revision = chunk.ContentRevision,
            Content = chunk.Content,
            EmbeddingJson = chunk.EmbeddingJson,
            EditedAt = DateTime.Now
        });
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
        HasEmbedding = !string.IsNullOrWhiteSpace(c.EmbeddingJson),
        ContentRevision = c.ContentRevision
    };
}

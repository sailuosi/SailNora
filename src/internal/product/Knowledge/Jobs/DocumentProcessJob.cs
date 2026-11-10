using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Options;
using SailNora.Foundation.Storage;
using SailNora.Product.Knowledge.DBLayer;
using SailNora.Product.Knowledge.Dtos;
using SailNora.Product.Knowledge.DocReader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SailNora.Product.Knowledge.Jobs;

/// <summary>对齐上游 asynq <c>document:process</c>。</summary>
public sealed class DocumentProcessJob : IScoped
{
    private readonly MooRepository<KnKnowledge> _knowledges;
    private readonly MooRepository<KnChunk> _chunks;
    private readonly ILocalFileStorage _storage;
    private readonly IDocReaderClient _docReader;
    private readonly DocReaderOptions _docReaderOptions;
    private readonly ILogger<DocumentProcessJob> _logger;

    public DocumentProcessJob(
        MooRepository<KnKnowledge> knowledges,
        MooRepository<KnChunk> chunks,
        ILocalFileStorage storage,
        IDocReaderClient docReader,
        IOptions<DocReaderOptions> docReaderOptions,
        ILogger<DocumentProcessJob> logger)
    {
        _knowledges = knowledges;
        _chunks = chunks;
        _storage = storage;
        _docReader = docReader;
        _docReaderOptions = docReaderOptions.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(DocumentProcessPayload payload, CancellationToken cancellationToken = default)
    {
        var knowledge = _knowledges.Repo.GetById(payload.KnowledgeId);
        if (knowledge is null)
        {
            _logger.LogWarning("document:process knowledge missing {KnowledgeId}", payload.KnowledgeId);
            return;
        }

        knowledge.ParseStatus = ParseStatuses.Processing;
        knowledge.ErrorMessage = null;
        _knowledges.Repo.Update(knowledge);

        try
        {
            var markdown = await ReadMarkdownAsync(payload, cancellationToken);
            var pieces = SimpleChunk(markdown, maxChars: 1200);
            if (pieces.Count == 0)
                pieces.Add("(empty)");

            var existing = _chunks.Repo.GetList(c => c.KnowledgeId == knowledge.Id);
            foreach (var old in existing)
                _chunks.Repo.Delete(old);

            var now = DateTime.Now;
            for (var i = 0; i < pieces.Count; i++)
            {
                _chunks.Repo.Insert(new KnChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    SpaceId = knowledge.SpaceId,
                    KnowledgeBaseId = knowledge.KnowledgeBaseId,
                    KnowledgeId = knowledge.Id,
                    Content = pieces[i],
                    ChunkIndex = i,
                    ChunkType = "text",
                    CreateTime = now
                });
            }

            knowledge.ParseStatus = ParseStatuses.Completed;
            knowledge.ProcessedAt = now;
            knowledge.ErrorMessage = null;
            _knowledges.Repo.Update(knowledge);
            _logger.LogInformation(
                "document:process completed {KnowledgeId} chunks={Count}",
                knowledge.Id, pieces.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "document:process failed {KnowledgeId}", knowledge.Id);
            knowledge.ParseStatus = ParseStatuses.Failed;
            knowledge.ErrorMessage = ex.Message;
            _knowledges.Repo.Update(knowledge);
            throw;
        }
    }

    private async Task<string> ReadMarkdownAsync(DocumentProcessPayload payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.FilePath))
            return string.Empty;

        if (_docReaderOptions.Enabled)
        {
            await using var stream = await _storage.OpenReadAsync(payload.FilePath, cancellationToken);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            var result = await _docReader.ReadAsync(
                ms.ToArray(),
                payload.FileName ?? "file",
                payload.FileType,
                payload.RequestId,
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.Markdown))
                return result.Markdown;
            if (!string.IsNullOrWhiteSpace(result.Error))
                _logger.LogWarning("docreader error: {Error}", result.Error);
        }

        var physical = _storage.ResolvePhysicalPath(payload.FilePath);
        var text = await File.ReadAllTextAsync(physical, cancellationToken);
        return string.IsNullOrWhiteSpace(text)
            ? $"# {payload.FileName}\n\n(binary or empty file stub)"
            : text;
    }

    private static List<string> SimpleChunk(string markdown, int maxChars)
    {
        var normalized = markdown.Replace("\r\n", "\n").Trim();
        if (normalized.Length == 0)
            return [];

        var parts = new List<string>();
        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var buffer = new System.Text.StringBuilder();
        foreach (var p in paragraphs)
        {
            if (buffer.Length + p.Length + 2 > maxChars && buffer.Length > 0)
            {
                parts.Add(buffer.ToString().Trim());
                buffer.Clear();
            }

            if (p.Length > maxChars)
            {
                if (buffer.Length > 0)
                {
                    parts.Add(buffer.ToString().Trim());
                    buffer.Clear();
                }

                for (var i = 0; i < p.Length; i += maxChars)
                    parts.Add(p.Substring(i, Math.Min(maxChars, p.Length - i)));
                continue;
            }

            if (buffer.Length > 0)
                buffer.Append("\n\n");
            buffer.Append(p);
        }

        if (buffer.Length > 0)
            parts.Add(buffer.ToString().Trim());
        return parts;
    }
}

public interface IDocumentProcessEnqueue
{
    void Enqueue(DocumentProcessPayload payload);
}

/// <summary>Hangfire 关闭时同步执行，保证 F2 冒烟可完成。</summary>
public sealed class InlineDocumentProcessEnqueue : IDocumentProcessEnqueue, IScoped
{
    private readonly DocumentProcessJob _job;

    public InlineDocumentProcessEnqueue(DocumentProcessJob job) => _job = job;

    public void Enqueue(DocumentProcessPayload payload) =>
        _job.ExecuteAsync(payload).GetAwaiter().GetResult();
}

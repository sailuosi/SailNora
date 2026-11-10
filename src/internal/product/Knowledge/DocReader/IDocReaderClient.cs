namespace SailNora.Product.Knowledge.DocReader;

public sealed class DocReaderReadResult
{
    public string? Markdown { get; set; }
    public string? Error { get; set; }
}

public sealed class DocReaderInfoResult
{
    public bool Reachable { get; set; }
    public string? Version { get; set; }
    public string? Message { get; set; }
}

public interface IDocReaderClient
{
    Task<DocReaderInfoResult> GetInfoAsync(CancellationToken cancellationToken = default);

    Task<DocReaderReadResult> ReadAsync(
        byte[] fileContent,
        string fileName,
        string? fileType,
        string? requestId,
        CancellationToken cancellationToken = default);
}

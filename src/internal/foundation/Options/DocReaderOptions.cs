namespace SailNora.Foundation.Options;

public sealed class DocReaderOptions
{
    public const string SectionName = "DocReader";

    /// <summary>gRPC 地址，例如 http://127.0.0.1:50051。</summary>
    public string Address { get; set; } = "http://127.0.0.1:50051";

    public bool Enabled { get; set; }
}

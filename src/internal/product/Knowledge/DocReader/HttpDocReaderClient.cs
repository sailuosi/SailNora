using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SailNora.Foundation.Options;

namespace SailNora.Product.Knowledge.DocReader;

/// <summary>保留作对照；默认改用 <see cref="GrpcDocReaderClient"/>。</summary>
public sealed class HttpDocReaderClient : IDocReaderClient
{
    private readonly DocReaderOptions _options;
    private readonly ILogger<HttpDocReaderClient> _logger;

    public HttpDocReaderClient(IOptions<DocReaderOptions> options, ILogger<HttpDocReaderClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<DocReaderInfoResult> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _logger.LogDebug("HttpDocReaderClient probe skipped; use GrpcDocReaderClient");
        return Task.FromResult(new DocReaderInfoResult
        {
            Reachable = false,
            Message = _options.Enabled ? "use gRPC client" : "DocReader.Enabled=false"
        });
    }

    public Task<DocReaderReadResult> ReadAsync(
        byte[] fileContent,
        string fileName,
        string? fileType,
        string? requestId,
        CancellationToken cancellationToken = default)
    {
        _ = (fileContent, fileName, fileType, requestId, cancellationToken);
        return Task.FromResult(new DocReaderReadResult { Error = "use GrpcDocReaderClient" });
    }
}

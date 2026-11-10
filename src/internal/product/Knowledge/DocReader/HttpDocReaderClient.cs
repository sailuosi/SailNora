using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SailNora.Foundation.DI;
using SailNora.Foundation.Options;

namespace SailNora.Product.Knowledge.DocReader;

/// <summary>
/// F2 探针客户端：优先尝试 gRPC 风格 HTTP/JSON 占位；不可达时返回 Reachable=false。
/// 完整 gRPC codegen（docreader.proto）后续可替换本实现。
/// </summary>
public sealed class HttpDocReaderClient : IDocReaderClient, ISingleton
{
    private readonly HttpClient _http;
    private readonly DocReaderOptions _options;
    private readonly ILogger<HttpDocReaderClient> _logger;

    public HttpDocReaderClient(
        IHttpClientFactory httpClientFactory,
        IOptions<DocReaderOptions> options,
        ILogger<HttpDocReaderClient> logger)
    {
        _http = httpClientFactory.CreateClient("docreader");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocReaderInfoResult> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new DocReaderInfoResult { Reachable = false, Message = "DocReader.Enabled=false" };

        try
        {
            // 真实 docreader 为 gRPC；此处仅做 TCP/HTTP 可达性探测占位。
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            using var req = new HttpRequestMessage(HttpMethod.Get, _options.Address.TrimEnd('/') + "/");
            using var resp = await _http.SendAsync(req, cts.Token);
            return new DocReaderInfoResult
            {
                Reachable = true,
                Version = "unknown",
                Message = $"HTTP {(int)resp.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "docreader probe failed");
            return new DocReaderInfoResult { Reachable = false, Message = ex.Message };
        }
    }

    public Task<DocReaderReadResult> ReadAsync(
        byte[] fileContent,
        string fileName,
        string? fileType,
        string? requestId,
        CancellationToken cancellationToken = default)
    {
        // gRPC Read 未接线前：返回空，由 DocumentProcessJob 回退本地文本读取。
        _ = (fileContent, fileName, fileType, requestId, cancellationToken);
        return Task.FromResult(new DocReaderReadResult
        {
            Error = "gRPC DocReader.Read 尚未接线，已回退本地文本解析"
        });
    }
}

using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SailNora.Foundation.DI;
using SailNora.Foundation.Options;
using DocReaderSvc = Docreader.DocReader;

namespace SailNora.Product.Knowledge.DocReader;

[Singleton]
public sealed class GrpcDocReaderClient : IDocReaderClient, IAsyncDisposable
{
    private readonly DocReaderOptions _options;
    private readonly ILogger<GrpcDocReaderClient> _logger;
    private readonly object _gate = new();
    private GrpcChannel? _channel;
    private DocReaderSvc.DocReaderClient? _client;

    public GrpcDocReaderClient(IOptions<DocReaderOptions> options, ILogger<GrpcDocReaderClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocReaderInfoResult> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new DocReaderInfoResult { Reachable = false, Message = "DocReader.Enabled=false" };

        try
        {
            var client = GetClient();
            var resp = await client.GetInfoAsync(new Docreader.GetInfoRequest(), cancellationToken: cancellationToken);
            return new DocReaderInfoResult
            {
                Reachable = true,
                Version = resp.Version,
                Message = string.IsNullOrWhiteSpace(resp.CommitId) ? "ok" : $"commit={resp.CommitId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "docreader GetInfo failed");
            return new DocReaderInfoResult { Reachable = false, Message = ex.Message };
        }
    }

    public async Task<DocReaderReadResult> ReadAsync(
        byte[] fileContent,
        string fileName,
        string? fileType,
        string? requestId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return new DocReaderReadResult { Error = "DocReader.Enabled=false" };

        try
        {
            var client = GetClient();
            var req = new Docreader.ReadRequest
            {
                FileContent = Google.Protobuf.ByteString.CopyFrom(fileContent),
                FileName = fileName ?? "file",
                FileType = fileType ?? string.Empty,
                RequestId = requestId ?? Guid.NewGuid().ToString("N")
            };
            var resp = await client.ReadAsync(req, cancellationToken: cancellationToken);
            return new DocReaderReadResult
            {
                Markdown = resp.MarkdownContent,
                Error = string.IsNullOrWhiteSpace(resp.Error) ? null : resp.Error
            };
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "docreader Read RPC failed");
            return new DocReaderReadResult { Error = ex.Status.Detail };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "docreader Read failed");
            return new DocReaderReadResult { Error = ex.Message };
        }
    }

    private DocReaderSvc.DocReaderClient GetClient()
    {
        if (_client is not null)
            return _client;

        lock (_gate)
        {
            if (_client is not null)
                return _client;

            var address = string.IsNullOrWhiteSpace(_options.Address)
                ? "http://127.0.0.1:50051"
                : _options.Address;
            _channel = GrpcChannel.ForAddress(address);
            _client = new DocReaderSvc.DocReaderClient(_channel);
            return _client;
        }
    }

    public ValueTask DisposeAsync()
    {
        _channel?.Dispose();
        return ValueTask.CompletedTask;
    }
}

using Microsoft.Extensions.Options;
using SailNora.Foundation.DI;
using SailNora.Foundation.Options;

namespace SailNora.Foundation.Storage;

public interface ILocalFileStorage
{
    Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

    string ResolvePhysicalPath(string relativePath);
}

[Singleton]
public sealed class LocalFileStorage : ILocalFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "files")
            : options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string relativePath, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(relativePath);
        var physical = ResolvePhysicalPath(normalized);
        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        await using var fs = File.Create(physical);
        await content.CopyToAsync(fs, cancellationToken);
        return normalized;
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var physical = ResolvePhysicalPath(Normalize(relativePath));
        Stream stream = File.OpenRead(physical);
        return Task.FromResult(stream);
    }

    public string ResolvePhysicalPath(string relativePath)
    {
        var physical = Path.GetFullPath(Path.Combine(_root, Normalize(relativePath)));
        if (!physical.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("非法存储路径");
        return physical;
    }

    private static string Normalize(string relativePath) =>
        relativePath.Replace('\\', '/').TrimStart('/');
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Hosting;

namespace SailNora.Foundation.Configuration;

public static class MooJsonShardExtensions
{
    private static readonly string[] ExcludeJsonPrefixes = ["appsettings", "bundleconfig", "compilerconfig"];

    private static readonly string[] RuntimeJsonSuffixes =
    [
        "deps.json",
        "runtimeconfig.dev.json",
        "runtimeconfig.prod.json",
        "runtimeconfig.json",
        "staticwebassets.runtime.json",
        "nuget.dgspec.json",
        "project.assets.json",
        "MvcTestingAppManifest.json"
    ];

    public static IConfigurationBuilder AddMooJsonShards(
        this IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment,
        Action<MooJsonShardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(hostEnvironment);

        var options = new MooJsonShardOptions();
        configure?.Invoke(options);
        AddJsonShards(configurationBuilder, hostEnvironment, options);
        return configurationBuilder;
    }

    private static void AddJsonShards(
        IConfigurationBuilder configurationBuilder,
        IHostEnvironment hostEnvironment,
        MooJsonShardOptions options)
    {
        var configuration = configurationBuilder is ConfigurationManager manager
            ? (IConfiguration)manager
            : configurationBuilder.Build();

        var executeDirectory = AppContext.BaseDirectory;
        var contentRoot = hostEnvironment.ContentRootPath;

        var scanEntries = (configuration.GetSection("ConfigurationScanDirectories").Get<string[]>() ?? [])
            .Concat(options.ScanDirectories);

        var scanDirectories = ResolveScanDirectories(executeDirectory, contentRoot, scanEntries);
        if (scanDirectories.Count == 0)
            return;

        var jsonFiles = scanDirectories
            .SelectMany(dir => Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (jsonFiles.Length == 0)
            return;

        var envName = hostEnvironment.EnvironmentName;
        if (string.IsNullOrWhiteSpace(envName))
            envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var ignoreConfigurationFiles = (configuration.GetSection("IgnoreConfigurationFiles").Get<string[]>() ?? [])
            .Concat(options.IgnoreFiles)
            .ToArray();

        var jsonFilesGroups = SplitConfigFileNameToGroups(jsonFiles)
            .Where(g => !ExcludeJsonPrefixes.Contains(g.Key, StringComparer.OrdinalIgnoreCase)
                        && !g.Any(c => IsExcludedFile(c, ignoreConfigurationFiles)));

        foreach (var group in jsonFilesGroups)
        {
            var limitFileNames = new[] { $"{group.Key}.json", $"{group.Key}.{envName}.json" };
            var files = group
                .Where(u => limitFileNames.Contains(Path.GetFileName(u), StringComparer.OrdinalIgnoreCase))
                .GroupBy(u => Path.GetFileName(u), StringComparer.OrdinalIgnoreCase)
                .Select(g => PreferContentRootFile(g, contentRoot, executeDirectory))
                .OrderBy(u => Path.GetFileName(u).Length);

            foreach (var jsonFile in files)
                configurationBuilder.AddJsonFile(jsonFile, optional: true, reloadOnChange: options.ReloadOnChange);
        }
    }

    private static List<string> ResolveScanDirectories(
        string executeDirectory,
        string? contentRoot,
        IEnumerable<string> scanEntries)
    {
        var roots = new List<string>();
        AddExistingDirectory(roots, executeDirectory);
        if (!string.IsNullOrWhiteSpace(contentRoot))
            AddExistingDirectory(roots, contentRoot);

        var result = new List<string>(roots);
        foreach (var entry in scanEntries)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;
            if (Path.IsPathRooted(entry))
            {
                AddExistingDirectory(result, entry);
                continue;
            }

            foreach (var root in roots)
                AddExistingDirectory(result, Path.Combine(root, entry));
        }

        return result.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IEnumerable<IGrouping<string, string>> SplitConfigFileNameToGroups(IEnumerable<string> configFiles)
    {
        return configFiles.GroupBy(static file =>
        {
            var fileNameParts = Path.GetFileName(file).Split('.', StringSplitOptions.RemoveEmptyEntries);
            return fileNameParts.Length == 2
                ? fileNameParts[0]
                : string.Join('.', fileNameParts.Take(fileNameParts.Length - 2));
        });
    }

    private static string PreferContentRootFile(
        IEnumerable<string> candidates,
        string? contentRoot,
        string executeDirectory)
    {
        var list = candidates.ToList();
        if (list.Count == 1)
            return list[0];

        if (!string.IsNullOrWhiteSpace(contentRoot))
        {
            var fromContent = list.FirstOrDefault(f => IsUnderRoot(f, contentRoot));
            if (fromContent != null)
                return fromContent;
        }

        return list.FirstOrDefault(f => IsUnderRoot(f, executeDirectory)) ?? list[0];
    }

    private static bool IsExcludedFile(string filePath, IEnumerable<string> ignoreConfigurationFiles)
    {
        var fileName = Path.GetFileName(filePath);
        if (RuntimeJsonSuffixes.Any(z => filePath.EndsWith(z, StringComparison.OrdinalIgnoreCase)))
            return true;
        if (ignoreConfigurationFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            return true;
        return ignoreConfigurationFiles.Any(i =>
        {
            if (string.IsNullOrWhiteSpace(i))
                return false;
            var matcher = new Matcher();
            matcher.AddInclude(i);
            return matcher.Match(fileName).HasMatches;
        });
    }

    private static void AddExistingDirectory(List<string> list, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        try
        {
            var full = Path.GetFullPath(path);
            if (Directory.Exists(full))
                list.Add(full);
        }
        catch
        {
            // ignore
        }
    }

    private static bool IsUnderRoot(string file, string root)
    {
        try
        {
            var fullFile = Path.GetFullPath(file);
            var fullRoot = Path.GetFullPath(root)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullFile.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

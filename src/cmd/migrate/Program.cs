using Npgsql;

static string? GetArg(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }

    return null;
}

static bool HasFlag(string[] args, string name) =>
    args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

var cs = GetArg(args, "--connection");
if (string.IsNullOrWhiteSpace(cs))
    cs = Environment.GetEnvironmentVariable("SAILNORA_PG");
var migrationsDir = GetArg(args, "--dir");
var dryRun = HasFlag(args, "--dry-run");
var toVersion = GetArg(args, "--to"); // e.g. 000010

if (string.IsNullOrWhiteSpace(migrationsDir))
{
    // 默认：相对本工具 → src/sync/migrations/versioned
    var baseDir = AppContext.BaseDirectory;
    migrationsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "sync", "migrations", "versioned"));
}

if (string.IsNullOrWhiteSpace(cs))
{
    Console.Error.WriteLine("""
        SailNora.Migrate — 应用 src/sync/migrations/versioned/*.up.sql 到 PostgreSQL

        用法:
          dotnet run --project src/cmd/migrate -f net10.0 -- --connection "Host=...;Database=...;Username=...;Password=..." [--dir PATH] [--dry-run] [--to 000010]

        或设置环境变量 SAILNORA_PG。
        """);
    return 2;
}

if (!Directory.Exists(migrationsDir))
{
    Console.Error.WriteLine($"migrations 目录不存在: {migrationsDir}");
    return 3;
}

var files = Directory.GetFiles(migrationsDir, "*.up.sql")
    .Select(f => new FileInfo(f))
    .OrderBy(f => f.Name, StringComparer.Ordinal)
    .ToList();

if (!string.IsNullOrWhiteSpace(toVersion))
{
    files = files.Where(f => string.CompareOrdinal(Path.GetFileNameWithoutExtension(f.Name).Replace(".up", ""), toVersion) <= 0)
        .ToList();
}

Console.WriteLine($"migrations dir: {migrationsDir}");
Console.WriteLine($"files: {files.Count}; dry-run={dryRun}");

await using var conn = new NpgsqlConnection(cs);
await conn.OpenAsync();

await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS sailnora_schema_migrations (
          version TEXT PRIMARY KEY,
          applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );
        """;
    if (!dryRun)
        await cmd.ExecuteNonQueryAsync();
    else
        Console.WriteLine("[dry-run] ensure sailnora_schema_migrations");
}

var applied = new HashSet<string>(StringComparer.Ordinal);
await using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT version FROM sailnora_schema_migrations";
    try
    {
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            applied.Add(reader.GetString(0));
    }
    catch when (dryRun)
    {
        // table may not exist yet in dry-run without create
    }
}

var pending = 0;
foreach (var file in files)
{
    var version = Path.GetFileNameWithoutExtension(file.Name); // 000001_xxx.up
    if (version.EndsWith(".up", StringComparison.OrdinalIgnoreCase))
        version = version[..^3];

    if (applied.Contains(version))
        continue;

    pending++;
    Console.WriteLine($"→ apply {file.Name}");
    if (dryRun)
        continue;

    var sql = await File.ReadAllTextAsync(file.FullName);
    await using var tx = await conn.BeginTransactionAsync();
    try
    {
        await using (var apply = conn.CreateCommand())
        {
            apply.Transaction = tx;
            apply.CommandText = sql;
            apply.CommandTimeout = 0;
            await apply.ExecuteNonQueryAsync();
        }

        await using (var mark = conn.CreateCommand())
        {
            mark.Transaction = tx;
            mark.CommandText = "INSERT INTO sailnora_schema_migrations(version) VALUES (@v)";
            mark.Parameters.AddWithValue("v", version);
            await mark.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        Console.WriteLine($"  ok {version}");
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        Console.Error.WriteLine($"  FAILED {version}: {ex.Message}");
        return 1;
    }
}

Console.WriteLine(pending == 0 ? "already up to date" : $"done, applied/pending={pending}");
return 0;

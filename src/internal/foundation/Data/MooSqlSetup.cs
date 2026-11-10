using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using mooSQL.config;
using mooSQL.data;
using mooSQL.data.context;

namespace SailNora.Foundation.Data;

public static class MooSqlSetup
{
    public const int DefaultPosition = 0;

    public static IServiceCollection AddMooSql(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DBCoonfigOptions>(configuration.GetSection(DBCoonfigOptions.SectionName));
        services.AddSingleton<DBInsCash>(CreateCash);
        services.AddScoped(typeof(MooRepository<>));
        return services;
    }

    private static DBInsCash CreateCash(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<DBCoonfigOptions>>().Value;
        if (options.Connections.Count == 0)
            throw new InvalidOperationException("DBCoonfig:Connections 不能为空。");

        var env = sp.GetService<IHostEnvironment>();
        EnsureSailNoraAssembliesLoaded();
        var cash = new DBClientBuilder()
            .registerGeneratedMaterializers()
            .doBuild();

        foreach (var conn in options.Connections)
        {
            if (string.Equals(conn.DbType, "SQLITE", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(conn.DbType, "SQLite", StringComparison.OrdinalIgnoreCase))
            {
                conn.ConnectString = EnsureSqliteDataDirectory(conn.ConnectString, env);
            }
        }

        cash.addConfig(options.Connections);

        if (env != null)
            cash.getClient().useWatchor(new MooSqlQueryWatchor(env));

        DBCash.Bind(cash);
        return cash;
    }

    internal static string EnsureSqliteDataDirectory(string connectString, IHostEnvironment? env)
    {
        if (string.IsNullOrWhiteSpace(connectString))
            return connectString;

        const string prefix = "Data Source=";
        var parts = connectString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rebuilt = new List<string>(parts.Length);
        var changed = false;

        foreach (var part in parts)
        {
            if (!part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                rebuilt.Add(part);
                continue;
            }

            var dataSource = part[prefix.Length..].Trim().Trim('"');
            if (string.IsNullOrEmpty(dataSource) ||
                dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
                Path.IsPathRooted(dataSource))
            {
                if (Path.IsPathRooted(dataSource))
                {
                    var parent = Path.GetDirectoryName(dataSource);
                    if (!string.IsNullOrEmpty(parent))
                        Directory.CreateDirectory(parent);
                }

                rebuilt.Add(part);
                continue;
            }

            var root = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
            var absolute = Path.GetFullPath(Path.Combine(root, dataSource));
            var dir = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            rebuilt.Add(prefix + absolute);
            changed = true;
        }

        return changed ? string.Join(';', rebuilt) : connectString;
    }

    internal static void EnsureSailNoraAssembliesLoaded()
    {
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var n = asm.GetName().Name;
            if (!string.IsNullOrEmpty(n))
                loaded.Add(n);
        }

        var entry = Assembly.GetEntryAssembly();
        if (entry == null)
            return;

        foreach (var name in entry.GetReferencedAssemblies())
        {
            if (name.Name == null
                || !name.Name.StartsWith("SailNora.", StringComparison.OrdinalIgnoreCase)
                || name.Name.Equals("SailNora.Foundation", StringComparison.OrdinalIgnoreCase))
                continue;
            if (loaded.Contains(name.Name))
                continue;
            try
            {
                Assembly.Load(name);
            }
            catch
            {
                // optional
            }
        }
    }
}

internal sealed class MooSqlQueryWatchor : Watchor
{
    private readonly IHostEnvironment _host;

    public MooSqlQueryWatchor(IHostEnvironment host) => _host = host;

    public override string onAfterExecuteSetError(string oprationId, ExeContext context, Exception ex, string operation)
    {
        var msg = $"SQL执行错误: {context.cmd.cmdText} | {ex.Message}";
        Console.WriteLine(msg);
        try
        {
            var dir = Path.Combine(_host.ContentRootPath, "log", "moosql");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"errSql{DateTime.Now:yyyyMMdd}.txt");
            File.AppendAllText(file, msg + Environment.NewLine);
        }
        catch
        {
            // ignore
        }

        return msg;
    }
}

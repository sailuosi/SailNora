using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Mcp.DBLayer;

namespace SailNora.Product.Mcp.Bootstrap;

public sealed class McpSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes =
    [
        typeof(KnMcpService),
        typeof(KnMcpEndpoint),
        typeof(KnHitlPending)
    ];

    private readonly DBInsCash _cash;

    public McpSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Sandbox.DBLayer;

namespace SailNora.Product.Sandbox.Bootstrap;

[Scoped]
public sealed class SandboxSchemaInitializer
{
    private static readonly Type[] EntityTypes = [typeof(KnSandboxConfig)];

    private readonly DBInsCash _cash;

    public SandboxSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

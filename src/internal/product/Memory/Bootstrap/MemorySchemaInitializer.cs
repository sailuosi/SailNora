using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Memory.DBLayer;

namespace SailNora.Product.Memory.Bootstrap;

[Scoped]
public sealed class MemorySchemaInitializer
{
    private static readonly Type[] EntityTypes = [typeof(KnMemory)];

    private readonly DBInsCash _cash;

    public MemorySchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Im.DBLayer;

namespace SailNora.Product.Im.Bootstrap;

[Scoped]
public sealed class ImSchemaInitializer
{
    private static readonly Type[] EntityTypes =
    [
        typeof(KnImChannel),
        typeof(KnImInboundEvent)
    ];

    private readonly DBInsCash _cash;

    public ImSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

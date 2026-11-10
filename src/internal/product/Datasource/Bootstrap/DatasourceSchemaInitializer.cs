using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Datasource.DBLayer;

namespace SailNora.Product.Datasource.Bootstrap;

public sealed class DatasourceSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes = [typeof(KnDatasource)];

    private readonly DBInsCash _cash;

    public DatasourceSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

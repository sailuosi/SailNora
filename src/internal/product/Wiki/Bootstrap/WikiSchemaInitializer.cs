using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Wiki.DBLayer;

namespace SailNora.Product.Wiki.Bootstrap;

public sealed class WikiSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes = [typeof(KnWikiPage)];

    private readonly DBInsCash _cash;

    public WikiSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

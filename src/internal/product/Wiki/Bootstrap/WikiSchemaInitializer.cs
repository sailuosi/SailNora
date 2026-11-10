using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Wiki.DBLayer;

namespace SailNora.Product.Wiki.Bootstrap;

[Scoped]
public sealed class WikiSchemaInitializer
{
    private static readonly Type[] EntityTypes = [typeof(KnWikiPage)];

    private readonly DBInsCash _cash;

    public WikiSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        EnsureColumn(db, "KnWikiPage", "Kwp_Status", "TEXT NOT NULL DEFAULT 'draft'");
        return Task.CompletedTask;
    }

    private static void EnsureColumn(DBInstance db, string table, string column, string ddlType)
    {
        try
        {
            var executor = new DBExecutor(db);
            executor.ExeNonQuery(new SQLCmd(
                $"ALTER TABLE {table} ADD COLUMN {column} {ddlType}",
                new Paras()));
        }
        catch
        {
            // column already exists
        }
    }
}

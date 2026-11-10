using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Knowledge.DBLayer;

namespace SailNora.Product.Knowledge.Bootstrap;

/// <summary>产品知识表 DDL（与平台 DbInitializer 分离，遵守依赖方向）。</summary>
[Scoped]
public sealed class KnowledgeSchemaInitializer
{
    private static readonly Type[] EntityTypes =
    [
        typeof(KnKnowledgeBase),
        typeof(KnKnowledge),
        typeof(KnChunk),
        typeof(KnChunkRevision),
        typeof(KnKnowledgeTag),
        typeof(KnFaqEntry)
    ];

    private readonly DBInsCash _cash;

    public KnowledgeSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        EnsureColumn(db, "KnChunk", "Kc_ContentRevision", "INTEGER NOT NULL DEFAULT 0");
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

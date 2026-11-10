using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Knowledge.DBLayer;

namespace SailNora.Product.Knowledge.Bootstrap;

/// <summary>产品知识表 DDL（与平台 DbInitializer 分离，遵守依赖方向）。</summary>
public sealed class KnowledgeSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes =
    [
        typeof(KnKnowledgeBase),
        typeof(KnKnowledge),
        typeof(KnChunk)
    ];

    private readonly DBInsCash _cash;

    public KnowledgeSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

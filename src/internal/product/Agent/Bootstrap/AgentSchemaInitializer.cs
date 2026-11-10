using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Agent.DBLayer;

namespace SailNora.Product.Agent.Bootstrap;

public sealed class AgentSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes = [typeof(KnAgent)];

    private readonly DBInsCash _cash;

    public AgentSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

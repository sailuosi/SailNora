using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Embed.DBLayer;

namespace SailNora.Product.Embed.Bootstrap;

public sealed class EmbedSchemaInitializer : IScoped
{
    private static readonly Type[] EntityTypes = [typeof(KnEmbedChannel)];

    private readonly DBInsCash _cash;

    public EmbedSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

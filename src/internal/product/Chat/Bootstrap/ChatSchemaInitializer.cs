using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Product.Chat.DBLayer;

namespace SailNora.Product.Chat.Bootstrap;

[Scoped]
public sealed class ChatSchemaInitializer
{
    private static readonly Type[] EntityTypes =
    [
        typeof(KnSession),
        typeof(KnMessage)
    ];

    private readonly DBInsCash _cash;

    public ChatSchemaInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        return Task.CompletedTask;
    }
}

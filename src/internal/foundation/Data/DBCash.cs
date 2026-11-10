using mooSQL.data;
using mooSQL.linq;

namespace SailNora.Foundation.Data;

public static class DBCash
{
    private static DBInsCash? _cash;

    public static void Bind(DBInsCash cash) => _cash = cash ?? throw new ArgumentNullException(nameof(cash));

    public static DBInsCash RequireCash() =>
        _cash ?? throw new InvalidOperationException("DBCash 未初始化，请先调用 services.AddMooSql(configuration)。");

    public static DBInstance GetDBInstance(int position) => RequireCash().getInstance(position);

    public static SQLBuilder useSQL(int position) => GetDBInstance(position).useSQL();

    public static SooRepository<T> useRepo<T>(int position) where T : class, new() =>
        GetDBInstance(position).useRepo<T>();

    public static SooUnitOfWork useWork(int position) => GetDBInstance(position).useWork();

    public static DDLBuilder useDDL(int position) => new DDLBuilder(GetDBInstance(position));
}

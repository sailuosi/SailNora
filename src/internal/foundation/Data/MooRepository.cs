using mooSQL.data;

namespace SailNora.Foundation.Data;

public class MooRepository<T> where T : class, new()
{
    private readonly DBInsCash _cash;
    private readonly int _position;

    public MooRepository(DBInsCash cash, int position = MooSqlSetup.DefaultPosition)
    {
        _cash = cash;
        _position = position;
    }

    public SooRepository<T> Repo => _cash.getInstance(_position).useRepo<T>();

    public DBInstance Db => _cash.getInstance(_position);
}

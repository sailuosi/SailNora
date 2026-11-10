using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Bootstrap;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Security;
using SailNora.Platform.Auth.DBLayer;
using SailNora.Platform.User.DBLayer;

namespace SailNora.Platform.Bootstrap;

public sealed class DbInitializer : IDbInitializer, IScoped
{
    private static readonly Type[] EntityTypes =
    [
        typeof(SysUser),
        typeof(SysRefreshToken)
    ];

    private static readonly Guid SuperAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly DBInsCash _cash;

    public DbInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        var executor = new DBExecutor(db);
        executor.ExeQueryScalar<int>(new SQLCmd(db.dialect.sentence.getPingSQL(), new Paras()));

        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        SeedIfEmpty(db);
        return Task.CompletedTask;
    }

    private static void SeedIfEmpty(DBInstance db)
    {
        var userRepo = db.useRepo<SysUser>();
        if (userRepo.Count(_ => true) > 0)
            return;

        var admin = new SysUser
        {
            Id = SuperAdminUserId,
            Account = "admin",
            Password = PasswordHasher.Hash("admin123"),
            RealName = "超级管理员",
            Status = UserStatus.Enabled,
            CreateTime = DateTime.Now
        };

        var work = db.useWork();
        work.Insert(admin);
        work.Commit();
    }
}

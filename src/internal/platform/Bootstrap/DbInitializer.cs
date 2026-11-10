using mooSQL.data;
using mooSQL.data.model;
using SailNora.Foundation.Bootstrap;
using SailNora.Foundation.Data;
using SailNora.Foundation.DI;
using SailNora.Foundation.Security;
using SailNora.Platform.Auth.DBLayer;
using SailNora.Platform.Space.DBLayer;
using SailNora.Platform.User.DBLayer;

namespace SailNora.Platform.Bootstrap;

[Scoped]
public sealed class DbInitializer : IDbInitializer
{
    private static readonly Type[] EntityTypes =
    [
        typeof(SysUser),
        typeof(SysRefreshToken),
        typeof(SysSpace),
        typeof(SysSpaceMember),
        typeof(SysApiKey)
    ];

    public static readonly Guid SuperAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultSpaceId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    private readonly DBInsCash _cash;

    public DbInitializer(DBInsCash cash) => _cash = cash;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var db = _cash.getInstance(MooSqlSetup.DefaultPosition);
        var executor = new DBExecutor(db);
        executor.ExeQueryScalar<int>(new SQLCmd(db.dialect.sentence.getPingSQL(), new Paras()));

        db.useDDL().doInitTable(new DMLOption(), EntityTypes);
        SeedIfEmpty(db);
        EnsureDefaultSpace(db);
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

    private static void EnsureDefaultSpace(DBInstance db)
    {
        var spaceRepo = db.useRepo<SysSpace>();
        var memberRepo = db.useRepo<SysSpaceMember>();
        var now = DateTime.Now;

        if (spaceRepo.GetById(DefaultSpaceId) is null)
        {
            var work = db.useWork();
            work.Insert(new SysSpace
            {
                Id = DefaultSpaceId,
                Name = "默认空间",
                Slug = "default",
                Status = SpaceStatus.Enabled,
                CreateTime = now
            });
            work.Commit();
        }

        var adminMember = memberRepo.GetFirst(m =>
            m.SpaceId == DefaultSpaceId && m.UserId == SuperAdminUserId);
        if (adminMember is null && db.useRepo<SysUser>().GetById(SuperAdminUserId) is not null)
        {
            var work = db.useWork();
            work.Insert(new SysSpaceMember
            {
                Id = Guid.NewGuid(),
                SpaceId = DefaultSpaceId,
                UserId = SuperAdminUserId,
                Role = SpaceRoles.Owner,
                CreateTime = now
            });
            work.Commit();
        }
    }
}

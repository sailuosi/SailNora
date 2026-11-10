using mooSQL.data;

namespace SailNora.Platform.User.DBLayer;

[SooTable("SysUser", Caption = "系统用户")]
public class SysUser
{
    [SooColumn("Su_Id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SooColumn("Su_Account", Length = 64, CanBeNull = false)]
    public string Account { get; set; } = string.Empty;

    [SooColumn("Su_Password", Length = 256, CanBeNull = false)]
    public string Password { get; set; } = string.Empty;

    [SooColumn("Su_RealName", Length = 64, CanBeNull = true)]
    public string? RealName { get; set; }

    [SooColumn("Su_Status", CanBeNull = false)]
    public int Status { get; set; } = 1;

    [SooColumn("Su_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

public static class UserStatus
{
    public const int Enabled = 1;
    public const int Disabled = 0;
}

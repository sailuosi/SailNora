using mooSQL.data;

namespace SailNora.Platform.Space.DBLayer;

[SooTable("SysSpaceMember", Caption = "空间成员")]
public class SysSpaceMember
{
    [SooColumn("Ssm_Id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SooColumn("Ssm_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Ssm_UserId", CanBeNull = false)]
    public Guid UserId { get; set; }

    /// <summary>owner | admin | contributor | viewer</summary>
    [SooColumn("Ssm_Role", Length = 32, CanBeNull = false)]
    public string Role { get; set; } = SpaceRoles.Viewer;

    [SooColumn("Ssm_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

public static class SpaceRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Contributor = "contributor";
    public const string Viewer = "viewer";
}

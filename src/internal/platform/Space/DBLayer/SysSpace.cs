using mooSQL.data;

namespace SailNora.Platform.Space.DBLayer;

[SooTable("SysSpace", Caption = "空间")]
public class SysSpace
{
    [SooColumn("Ss_Id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SooColumn("Ss_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Ss_Slug", Length = 64, CanBeNull = true)]
    public string? Slug { get; set; }

    [SooColumn("Ss_Status", CanBeNull = false)]
    public int Status { get; set; } = 1;

    [SooColumn("Ss_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

public static class SpaceStatus
{
    public const int Enabled = 1;
    public const int Disabled = 0;
}

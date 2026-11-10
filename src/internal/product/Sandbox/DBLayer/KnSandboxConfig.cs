using mooSQL.data;

namespace SailNora.Product.Sandbox.DBLayer;

[SooTable("KnSandboxConfig", Caption = "沙箱配置")]
public class KnSandboxConfig
{
    [SooColumn("Ksc_Id", IsPrimaryKey = true, Length = 36)]
    public string Id { get; set; } = string.Empty;

    [SooColumn("Ksc_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Ksc_Name", Length = 128, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Ksc_Description", Length = 512, CanBeNull = true)]
    public string? Description { get; set; }

    /// <summary>host | docker | remote（F5d 仅持久化配置，不真正起容器）</summary>
    [SooColumn("Ksc_Backend", Length = 32, CanBeNull = false)]
    public string Backend { get; set; } = "host";

    [SooColumn("Ksc_Image", Length = 256, CanBeNull = true)]
    public string? Image { get; set; }

    [SooColumn("Ksc_WorkDir", Length = 512, CanBeNull = true)]
    public string? WorkDir { get; set; }

    [SooColumn("Ksc_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Ksc_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }

    [SooColumn("Ksc_UpdateTime", CanBeNull = false)]
    public DateTime UpdateTime { get; set; }
}

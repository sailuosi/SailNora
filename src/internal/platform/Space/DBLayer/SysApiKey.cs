using mooSQL.data;

namespace SailNora.Platform.Space.DBLayer;

[SooTable("SysApiKey", Caption = "空间 API Key")]
public class SysApiKey
{
    [SooColumn("Sak_Id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SooColumn("Sak_SpaceId", CanBeNull = false)]
    public Guid SpaceId { get; set; }

    [SooColumn("Sak_Name", Length = 64, CanBeNull = false)]
    public string Name { get; set; } = string.Empty;

    [SooColumn("Sak_KeyHash", Length = 128, CanBeNull = false)]
    public string KeyHash { get; set; } = string.Empty;

    [SooColumn("Sak_Prefix", Length = 16, CanBeNull = false)]
    public string Prefix { get; set; } = string.Empty;

    [SooColumn("Sak_Enabled", CanBeNull = false)]
    public bool Enabled { get; set; } = true;

    [SooColumn("Sak_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

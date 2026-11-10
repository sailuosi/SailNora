using mooSQL.data;

namespace SailNora.Platform.Auth.DBLayer;

[SooTable("SysRefreshToken", Caption = "刷新令牌")]
public class SysRefreshToken
{
    [SooColumn("Srt_Id", IsPrimaryKey = true)]
    public Guid Id { get; set; }

    [SooColumn("Srt_UserId", CanBeNull = false)]
    public Guid UserId { get; set; }

    [SooColumn("Srt_TokenHash", Length = 128, CanBeNull = false)]
    public string TokenHash { get; set; } = string.Empty;

    [SooColumn("Srt_ExpireTime", CanBeNull = false)]
    public DateTime ExpireTime { get; set; }

    [SooColumn("Srt_IsRevoked", CanBeNull = false)]
    public bool IsRevoked { get; set; }

    [SooColumn("Srt_CreateTime", CanBeNull = false)]
    public DateTime CreateTime { get; set; }
}

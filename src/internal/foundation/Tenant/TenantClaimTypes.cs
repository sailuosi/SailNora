namespace SailNora.Foundation.Tenant;

public static class TenantClaimTypes
{
    public const string TenantId = "tid";

    /// <summary>JwtBearer 默认 Inbound 映射后的长名（MapInboundClaims=true 时）。</summary>
    public const string TenantIdLong =
        "http://schemas.microsoft.com/identity/claims/tenantid";
}

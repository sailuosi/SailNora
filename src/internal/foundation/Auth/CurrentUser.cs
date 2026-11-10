using System.Security.Claims;
using SailNora.Foundation.Tenant;

namespace SailNora.Foundation.Auth;

public static class CurrentUser
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var uid = user?.FindFirstValue("uid");
        if (!Guid.TryParse(uid, out var id) || id == Guid.Empty)
            return null;
        return id;
    }

    public static Guid? GetTenantId(ClaimsPrincipal? user)
    {
        var tid = user?.FindFirstValue(TenantClaimTypes.TenantId)
                  ?? user?.FindFirstValue(TenantClaimTypes.TenantIdLong)
                  ?? user?.FindFirstValue(SpaceClaimTypes.SpaceId);
        if (!Guid.TryParse(tid, out var id) || id == Guid.Empty)
            return null;
        return id;
    }

    public static string? GetSpaceRole(ClaimsPrincipal? user) =>
        user?.FindFirstValue(SpaceClaimTypes.SpaceRole);

    public static string? GetAccount(ClaimsPrincipal? user) =>
        user?.FindFirstValue(ClaimTypes.Name) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
}

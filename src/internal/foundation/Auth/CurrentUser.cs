using System.Security.Claims;
using SailNora.Foundation.Tenant;

namespace SailNora.Foundation.Auth;

public static class CurrentUser
{
    public static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var uid = user?.FindFirstValue("uid");
        return Guid.TryParse(uid, out var id) ? id : null;
    }

    public static Guid? GetTenantId(ClaimsPrincipal? user)
    {
        var tid = user?.FindFirstValue(TenantClaimTypes.TenantId);
        return Guid.TryParse(tid, out var id) ? id : null;
    }

    public static string? GetAccount(ClaimsPrincipal? user) =>
        user?.FindFirstValue(ClaimTypes.Name) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
}

using System.Security.Claims;

namespace SailNora.Foundation.Auth;

public static class AuthPrincipalHelper
{
    public static bool BypassesPermissionCheck(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var account = CurrentUser.GetAccount(user);
        return string.Equals(account, AuthRoleCodes.AdminAccount, StringComparison.OrdinalIgnoreCase);
    }
}

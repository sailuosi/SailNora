using Microsoft.AspNetCore.Authorization;
using SailNora.Foundation.Auth;
using SailNora.Foundation.DI;

namespace SailNora.App.Auth;

[Singleton]
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (AuthPrincipalHelper.BypassesPermissionCheck(context.User))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.Claims.Any(c =>
                c.Type == PermissionClaimTypes.Permission &&
                string.Equals(c.Value, requirement.RequiredPermission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

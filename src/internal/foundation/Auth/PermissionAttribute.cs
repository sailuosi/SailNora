using Microsoft.AspNetCore.Authorization;

namespace SailNora.Foundation.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permission) => Policy = PermissionPolicies.ToPolicyName(permission);
}

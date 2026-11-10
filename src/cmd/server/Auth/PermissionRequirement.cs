using Microsoft.AspNetCore.Authorization;

namespace SailNora.App.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string requiredPermission) => RequiredPermission = requiredPermission;

    public string RequiredPermission { get; }
}

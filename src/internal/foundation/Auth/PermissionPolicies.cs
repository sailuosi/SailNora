namespace SailNora.Foundation.Auth;

public static class PermissionPolicies
{
    public const string Prefix = "perm:";

    public static string ToPolicyName(string permission) => Prefix + permission;
}

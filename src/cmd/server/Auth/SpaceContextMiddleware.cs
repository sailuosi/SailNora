using System.Security.Claims;
using SailNora.Foundation.Models;
using SailNora.Foundation.Tenant;
using SailNora.Platform.Space.DBLayer;
using SailNora.Platform.Space.Service;

namespace SailNora.App.Auth;

/// <summary>
/// 解析 X-Tenant-ID（空间切换）与 X-API-Key（机器主体）。
/// </summary>
public sealed class SpaceContextMiddleware
{
    public const string ApiKeyHeader = "X-API-Key";
    public const string TenantHeader = "X-Tenant-ID";

    private readonly RequestDelegate _next;

    public SpaceContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, SpaceService spaces)
    {
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyHeader)
            && !string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            var key = spaces.FindApiKeyByRaw(apiKeyHeader.ToString());
            if (key is null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResult.Fail("无效 API Key", ApiResultCodes.Unauthorized));
                return;
            }

            var identity = new ClaimsIdentity("ApiKey");
            identity.AddClaim(new Claim(SpaceClaimTypes.SpaceId, key.SpaceId.ToString()));
            identity.AddClaim(new Claim(SpaceClaimTypes.SpaceRole, SpaceRoles.Admin));
            identity.AddClaim(new Claim(ClaimTypes.Name, "api-key:" + key.Prefix));
            identity.AddClaim(new Claim("uid", Guid.Empty.ToString()));
            identity.AddClaim(new Claim(SailNora.Foundation.Auth.PermissionClaimTypes.Permission, "skeleton:probe"));
            identity.AddClaim(new Claim(SailNora.Foundation.Auth.PermissionClaimTypes.Permission, "space:read"));
            context.User = new ClaimsPrincipal(identity);
        }
        else if (context.User.Identity?.IsAuthenticated == true
                 && context.Request.Headers.TryGetValue(TenantHeader, out var tenantHeader)
                 && Guid.TryParse(tenantHeader.ToString(), out var spaceId))
        {
            var userIdHeader = context.User.FindFirst("uid")?.Value;
            if (!Guid.TryParse(userIdHeader, out var userId) || userId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResult.Fail("无法切换空间", ApiResultCodes.Forbidden));
                return;
            }

            var membership = spaces.GetMembership(userId, spaceId);
            if (membership is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResult.Fail("非空间成员", ApiResultCodes.Forbidden));
                return;
            }

            var id = (ClaimsIdentity)context.User.Identity!;
            var existingTid = id.FindFirst(SpaceClaimTypes.SpaceId);
            if (existingTid is not null)
                id.RemoveClaim(existingTid);
            var existingRole = id.FindFirst(SpaceClaimTypes.SpaceRole);
            if (existingRole is not null)
                id.RemoveClaim(existingRole);
            id.AddClaim(new Claim(SpaceClaimTypes.SpaceId, spaceId.ToString()));
            id.AddClaim(new Claim(SpaceClaimTypes.SpaceRole, membership.Role));
        }

        await _next(context);
    }
}

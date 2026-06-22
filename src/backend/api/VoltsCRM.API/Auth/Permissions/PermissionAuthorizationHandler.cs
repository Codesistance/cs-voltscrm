using Microsoft.AspNetCore.Authorization;

namespace VoltsCRM.API.Auth.Permissions;

/// <summary>
/// Grants access when the signed JWT contains a matching <c>perm</c> claim. Permissions are resolved
/// at token issuance (including super-admin expansion), so this handler never touches the database —
/// authorization is a pure claim check on each request.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(AppClaims.Permission, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

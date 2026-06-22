using VoltsCRM.API.Auth.Permissions;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.API.Auth;

/// <summary>Fluent helpers for gating minimal-API endpoints by permission or user type.</summary>
public static class AuthorizationExtensions
{
    /// <summary>Requires the caller's token to carry the given permission (Administration users).</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization($"{PermissionPolicyProvider.Prefix}{permission}");

    /// <summary>Requires the caller to be of the given user type. Maps to the static <c>type:*</c> policy.</summary>
    public static TBuilder RequireUserType<TBuilder>(this TBuilder builder, UserType userType)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireAuthorization($"{UserTypePolicy.Prefix}{userType}");
}

/// <summary>Naming convention for the per-user-type authorization policies registered in Program.cs.</summary>
public static class UserTypePolicy
{
    public const string Prefix = "type:";

    public static string For(UserType userType) => $"{Prefix}{userType}";
}

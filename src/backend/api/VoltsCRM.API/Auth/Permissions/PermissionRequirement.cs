using Microsoft.AspNetCore.Authorization;

namespace VoltsCRM.API.Auth.Permissions;

/// <summary>Authorization requirement satisfied when the caller's token carries the named permission claim.</summary>
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

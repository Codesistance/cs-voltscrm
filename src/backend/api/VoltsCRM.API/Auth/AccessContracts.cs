namespace VoltsCRM.API.Auth;

public sealed record PermissionDto(string Key, string Group, string Description);

public sealed record AdminRoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UserCount);

public sealed record SaveAdminRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

public sealed record AdminUserDto(
    Guid Id,
    string UserId,
    string Email,
    string FullName,
    bool IsSuperAdmin,
    bool IsActive,
    IReadOnlyList<Guid> RoleIds);

public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);

public sealed record CreateAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<Guid> RoleIds,
    bool IsSuperAdmin,
    // Password mode chosen before creation: null/empty → the server generates a temporary password
    // and returns it once; a supplied value is set directly. Either way the account must change it
    // at next login. This avoids the create-then-reset dance.
    string? Password = null);

public sealed record CreateAdminResult(
    AdminUserDto Admin,
    // The generated temporary password, returned once when Password was not supplied; null when the
    // caller set their own value (nothing new to disclose).
    string? TemporaryPassword);

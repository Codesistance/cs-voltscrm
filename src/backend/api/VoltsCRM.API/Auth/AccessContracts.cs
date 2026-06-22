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
    IReadOnlyList<Guid> RoleIds);

public sealed record AssignRolesRequest(IReadOnlyList<Guid> RoleIds);

public sealed record CreateAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<Guid> RoleIds,
    bool IsSuperAdmin);

namespace VoltsCRM.Integration.Tests;

/// <summary>Minimal mirrors of the API response contracts for deserialization in tests.</summary>
public sealed record LoginResponseDto(string AccessToken, int ExpiresIn, AuthUserDtoT User, string? RefreshToken = null);

public sealed record RefreshResponseDto(string AccessToken, int ExpiresIn, string? RefreshToken = null);

public sealed record AuthUserDtoT(
    string Id,
    string Email,
    string FullName,
    string UserType,
    List<string> Roles,
    List<string> Permissions,
    bool MustChangePassword,
    bool IsSuperAdmin);

public sealed record PermissionDtoT(string Key, string Group, string Description);

public sealed record AdminRoleDtoT(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    List<string> Permissions,
    int UserCount);

public sealed record AdminUserDtoT(
    Guid Id,
    string UserId,
    string Email,
    string FullName,
    bool IsSuperAdmin,
    List<Guid> RoleIds);

public sealed record ResetPasswordResultDtoT(string? TemporaryPassword);

public sealed record CreateAdminResultT(AdminUserDtoT Admin, string? TemporaryPassword);

public sealed record PhoenixResetResultDtoT(string Email, string TemporaryPassword, bool Reactivated);

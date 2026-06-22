namespace VoltsCRM.API.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUserDto(
    string Id,
    string Email,
    string FullName,
    string UserType,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool MustChangePassword,
    bool IsSuperAdmin);

public sealed record LoginResponse(string AccessToken, int ExpiresIn, AuthUserDto User);

public sealed record RefreshResponse(string AccessToken, int ExpiresIn);

public sealed record SetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

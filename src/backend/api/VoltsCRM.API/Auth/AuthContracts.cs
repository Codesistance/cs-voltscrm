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

// RefreshToken is populated only in cookie-less mode (AuthOptions.RefreshTokenInBody);
// otherwise it stays null and the token rides in the httpOnly refresh cookie.
public sealed record LoginResponse(string AccessToken, int ExpiresIn, AuthUserDto User, string? RefreshToken = null);

public sealed record RefreshResponse(string AccessToken, int ExpiresIn, string? RefreshToken = null);

// Body sent to /auth/refresh (and /auth/logout) in cookie-less mode.
public sealed record RefreshRequest(string? RefreshToken);

public sealed record SetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

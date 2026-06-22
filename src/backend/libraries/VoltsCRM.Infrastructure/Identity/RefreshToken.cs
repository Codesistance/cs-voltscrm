namespace VoltsCRM.Infrastructure.Identity;

/// <summary>
/// Persisted, hashed refresh token for silent re-authentication. The raw token is sent to the
/// client in an httpOnly cookie; only its hash is stored here. Rotated on every use.
/// Lives in the <c>identity</c> schema alongside the ASP.NET Identity tables.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}

namespace VoltsCRM.Application.Authorization;

/// <summary>
/// Resolves the effective permission set for a user at token-issuance time. Returns an empty set for
/// non-Administration users; the union of all assigned roles' permissions for admins; and the full
/// catalogue for super admins.
/// </summary>
public interface IPermissionResolver
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(string userId, CancellationToken ct = default);
}

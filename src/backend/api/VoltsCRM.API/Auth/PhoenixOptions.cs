namespace VoltsCRM.API.Auth;

/// <summary>
/// Configuration for the Phoenix account-recovery endpoint. Bound from the "Phoenix" section
/// (env var <c>Phoenix__Enabled</c>, driven by the <c>enable_phoenix</c> tfvar in deployed
/// environments). Fail-closed: defaults to <c>false</c>, and when disabled the Phoenix endpoints
/// are not mapped at all, so the route returns 404 rather than existing but rejecting callers.
/// </summary>
public sealed class PhoenixOptions
{
    public const string SectionName = "Phoenix";

    /// <summary>When true, the super-admin account-recovery endpoint is exposed.</summary>
    public bool Enabled { get; set; }
}

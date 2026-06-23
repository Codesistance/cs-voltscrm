namespace VoltsCRM.Application.Common.Options;

/// <summary>
/// Seeded-admin credential configuration. Bind from the "Seed" section. The HMAC key is the secret
/// that makes the seeded admin's daily password unguessable; it must be supplied per-environment via
/// the <c>Seed__HmacKey</c> environment variable (sourced from AWS SSM Parameter Store in deployed
/// environments) and must never be hardcoded.
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Secret HMAC key used to derive the seeded admin's daily password (≥ 32 chars).</summary>
    public string HmacKey { get; set; } = string.Empty;
}

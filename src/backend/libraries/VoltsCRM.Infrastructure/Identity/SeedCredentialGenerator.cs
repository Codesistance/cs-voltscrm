using System.Security.Cryptography;
using System.Text;

namespace VoltsCRM.Infrastructure.Identity;

/// <summary>
/// Generates the seeded admin's password as a function of a secret HMAC key + the current date.
/// Password formula: <c>Base64(HMAC-SHA256(key, "ddMMyyyy"))[0..16] + "!Aa1"</c>.
/// <para>
/// The <b>key is the secret</b> and must be supplied from configuration (<c>Seed:HmacKey</c>, sourced
/// from a secrets manager / env var) — it is never hardcoded. The date only provides daily rotation,
/// not secrecy: with a known key the password would be trivially computable, so the key must stay out
/// of source and out of the shipped image.
/// </para>
/// </summary>
public static class SeedCredentialGenerator
{
    /// <summary>The well-known email for the seeded admin account.</summary>
    public const string SeededAdminEmail = "admin@voltscrm.local";

    /// <summary>Minimum accepted HMAC key length (characters).</summary>
    public const int MinKeyLength = 32;

    /// <summary>Computes the password for a given date (uses the UTC date component only).</summary>
    public static string ComputePassword(DateTime date, string key)
        => ComputePasswordFromDateString(date.ToString("ddMMyyyy"), key);

    /// <summary>Computes the password for a given date.</summary>
    public static string ComputePassword(DateOnly date, string key)
        => ComputePasswordFromDateString(date.ToString("ddMMyyyy"), key);

    /// <summary>Computes today's password (UTC).</summary>
    public static string ComputeTodaysPassword(string key) => ComputePassword(DateTime.UtcNow, key);

    private static string ComputePasswordFromDateString(string dateString, string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < MinKeyLength)
            throw new ArgumentException(
                $"Seed HMAC key must be at least {MinKeyLength} characters. Configure 'Seed:HmacKey' " +
                "via the Seed__HmacKey env var / AWS Secrets Manager — it must never be hardcoded.",
                nameof(key));

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dateBytes = Encoding.UTF8.GetBytes(dateString);
        var hash = HMACSHA256.HashData(keyBytes, dateBytes);

        // Take the first 16 chars of Base64 and append a suffix that satisfies the Identity password
        // policy (special char, uppercase, lowercase, digit).
        var truncated = Convert.ToBase64String(hash)[..16];
        return $"{truncated}!Aa1";
    }
}

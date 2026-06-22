using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VoltsCRM.Infrastructure.Integrations;

/// <summary>
/// Constant-time HMAC-SHA256 webhook signature verification with an optional replay/timestamp window.
/// This is the reference pattern real gateway adapters (M-Pesa/Stripe/...) should reuse — never the
/// always-true stub. The signature header is expected to be the lowercase hex HMAC of the raw body
/// (optionally prefixed with "t=&lt;unixSeconds&gt;,v1=&lt;hex&gt;" when a timestamp is supplied).
/// </summary>
public static class WebhookSignature
{
    /// <summary>Default replay window: reject signed timestamps older/newer than this.</summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Verifies <paramref name="signature"/> over <paramref name="payload"/> using <paramref name="secret"/>.
    /// When the signature carries a "t=" timestamp, also enforces the replay window against <paramref name="now"/>.
    /// </summary>
    public static bool Verify(string payload, string signature, string secret, DateTimeOffset? now = null, TimeSpan? tolerance = null)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
            return false;

        var (timestamp, providedHex) = Parse(signature);

        // Replay-window check (only when a timestamp is present in the header).
        if (timestamp is not null)
        {
            var window = tolerance ?? DefaultTolerance;
            var delta = (now ?? DateTimeOffset.UtcNow) - timestamp.Value;
            if (delta.Duration() > window)
                return false;
        }

        // When a timestamp is present, the signed message is "<unixSeconds>.<payload>" (Stripe-style).
        var signedPayload = timestamp is not null
            ? $"{timestamp.Value.ToUnixTimeSeconds()}.{payload}"
            : payload;

        var expectedHex = Compute(signedPayload, secret);

        var providedBytes = FromHex(providedHex);
        var expectedBytes = FromHex(expectedHex);
        if (providedBytes is null || expectedBytes is null || providedBytes.Length != expectedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    /// <summary>Computes the lowercase hex HMAC-SHA256 of <paramref name="message"/> under <paramref name="secret"/>.</summary>
    public static string Compute(string message, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static (DateTimeOffset? Timestamp, string Hex) Parse(string signature)
    {
        // Supports either a bare hex string, or "t=<unixSeconds>,v1=<hex>".
        if (!signature.Contains('='))
            return (null, signature.Trim());

        DateTimeOffset? ts = null;
        var hex = string.Empty;
        foreach (var part in signature.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            switch (kv[0])
            {
                case "t" when long.TryParse(kv[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var secs):
                    ts = DateTimeOffset.FromUnixTimeSeconds(secs);
                    break;
                case "v1":
                    hex = kv[1];
                    break;
            }
        }
        return (ts, hex);
    }

    private static byte[]? FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
            return null;
        try { return Convert.FromHexString(hex); }
        catch (FormatException) { return null; }
    }
}

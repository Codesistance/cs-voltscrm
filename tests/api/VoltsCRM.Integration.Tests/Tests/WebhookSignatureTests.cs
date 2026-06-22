using VoltsCRM.Infrastructure.Integrations;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Unit tests for <see cref="WebhookSignature"/> — the constant-time HMAC verification that real
/// gateway adapters use in place of the always-true stub (closes the S2 pattern).
/// </summary>
public class WebhookSignatureTests
{
    private const string Secret = "a-test-webhook-secret-value";
    private const string Payload = "{\"transactionReference\":\"VP-ABC\",\"status\":\"Completed\"}";

    [Fact]
    public void Verify_ValidBareHexSignature_ReturnsTrue()
    {
        var sig = WebhookSignature.Compute(Payload, Secret);
        Assert.True(WebhookSignature.Verify(Payload, sig, Secret));
    }

    [Fact]
    public void Verify_TamperedPayload_ReturnsFalse()
    {
        var sig = WebhookSignature.Compute(Payload, Secret);
        Assert.False(WebhookSignature.Verify(Payload + "x", sig, Secret));
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var sig = WebhookSignature.Compute(Payload, Secret);
        Assert.False(WebhookSignature.Verify(Payload, sig, "different-secret"));
    }

    [Fact]
    public void Verify_EmptyOrGarbageSignature_ReturnsFalse()
    {
        Assert.False(WebhookSignature.Verify(Payload, "", Secret));
        Assert.False(WebhookSignature.Verify(Payload, "not-hex-zz", Secret));
    }

    [Fact]
    public void Verify_TimestampedSignatureWithinWindow_ReturnsTrue()
    {
        var now = DateTimeOffset.UtcNow;
        var signed = $"{now.ToUnixTimeSeconds()}.{Payload}";
        var sig = $"t={now.ToUnixTimeSeconds()},v1={WebhookSignature.Compute(signed, Secret)}";
        Assert.True(WebhookSignature.Verify(Payload, sig, Secret, now));
    }

    [Fact]
    public void Verify_TimestampOutsideReplayWindow_ReturnsFalse()
    {
        var signedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var signed = $"{signedAt.ToUnixTimeSeconds()}.{Payload}";
        var sig = $"t={signedAt.ToUnixTimeSeconds()},v1={WebhookSignature.Compute(signed, Secret)}";
        Assert.False(WebhookSignature.Verify(Payload, sig, Secret, DateTimeOffset.UtcNow));
    }
}

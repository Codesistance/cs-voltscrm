using VoltsCRM.Infrastructure.Identity;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Unit tests for <see cref="SeedCredentialGenerator"/>. Pure/in-memory — no test container, so this
/// class deliberately does NOT join the "SharedTestContainers" collection.
/// </summary>
public class SeedCredentialGeneratorTests
{
    private const string Key = "test-seed-hmac-key-0123456789-abcdef"; // ≥ 32 chars
    private static readonly DateTime Date = new(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Same_date_and_key_produce_stable_password()
    {
        var a = SeedCredentialGenerator.ComputePassword(Date, Key);
        var b = SeedCredentialGenerator.ComputePassword(Date, Key);

        Assert.Equal(a, b);
        Assert.EndsWith("!Aa1", a);
        Assert.Equal(20, a.Length); // 16 base64 chars + "!Aa1"
    }

    [Fact]
    public void Different_key_produces_different_password()
    {
        var a = SeedCredentialGenerator.ComputePassword(Date, Key);
        var b = SeedCredentialGenerator.ComputePassword(Date, "a-completely-different-seed-hmac-key-987654321");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Different_date_produces_different_password()
    {
        var a = SeedCredentialGenerator.ComputePassword(Date, Key);
        var b = SeedCredentialGenerator.ComputePassword(Date.AddDays(1), Key);

        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    public void Missing_or_short_key_throws(string key)
        => Assert.Throws<ArgumentException>(() => SeedCredentialGenerator.ComputePassword(Date, key));
}

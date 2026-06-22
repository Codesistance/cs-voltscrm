using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Tests.Entities.Billing;

public class DiscountGrantTests
{
    private static DiscountGrant CreateGrant(DiscountType type, decimal value) =>
        new(Guid.NewGuid(), type, value, DiscountScope.Invoice, "admin");

    [Theory]
    [InlineData(10, 1000, 100)]   // 10% of 1000 = 100
    [InlineData(25, 400, 100)]    // 25% of 400 = 100
    [InlineData(50, 200, 100)]    // 50% of 200 = 100
    [InlineData(100, 500, 500)]   // 100% of 500 = 500
    public void CalculateDiscountAmount_percentage_calculates_correctly(decimal percentage, decimal gross, decimal expected)
    {
        var grant = CreateGrant(DiscountType.Percentage, percentage);

        var result = grant.CalculateDiscountAmount(gross);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100, 500, 100)]   // Fixed 100 off 500 = 100
    [InlineData(200, 500, 200)]   // Fixed 200 off 500 = 200
    [InlineData(600, 500, 500)]   // Fixed 600 off 500 = 500 (capped)
    public void CalculateDiscountAmount_fixed_calculates_correctly(decimal fixedAmount, decimal gross, decimal expected)
    {
        var grant = CreateGrant(DiscountType.FixedAmount, fixedAmount);

        var result = grant.CalculateDiscountAmount(gross);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsActiveAt_returns_true_within_validity_period()
    {
        var now = DateTimeOffset.UtcNow;
        var grant = new DiscountGrant(
            Guid.NewGuid(), DiscountType.Percentage, 10, DiscountScope.Invoice, "admin",
            validFrom: now.AddDays(-1), validUntil: now.AddDays(1));

        Assert.True(grant.IsActiveAt(now));
    }

    [Fact]
    public void IsActiveAt_returns_false_before_valid_from()
    {
        var now = DateTimeOffset.UtcNow;
        var grant = new DiscountGrant(
            Guid.NewGuid(), DiscountType.Percentage, 10, DiscountScope.Invoice, "admin",
            validFrom: now.AddDays(1));

        Assert.False(grant.IsActiveAt(now));
    }

    [Fact]
    public void IsActiveAt_returns_false_after_valid_until()
    {
        var now = DateTimeOffset.UtcNow;
        var grant = new DiscountGrant(
            Guid.NewGuid(), DiscountType.Percentage, 10, DiscountScope.Invoice, "admin",
            validFrom: now.AddDays(-10), validUntil: now.AddDays(-1));

        Assert.False(grant.IsActiveAt(now));
    }

    [Fact]
    public void MarkApplied_changes_status_for_non_recurring()
    {
        var grant = CreateGrant(DiscountType.Percentage, 10);

        grant.MarkApplied();

        Assert.Equal(DiscountGrantStatus.Applied, grant.Status);
    }

    [Fact]
    public void MarkApplied_keeps_active_for_recurring()
    {
        var grant = new DiscountGrant(
            Guid.NewGuid(), DiscountType.Percentage, 10, DiscountScope.Invoice, "admin",
            isRecurring: true);

        grant.MarkApplied();

        Assert.Equal(DiscountGrantStatus.Active, grant.Status);
    }

    [Fact]
    public void Revoke_changes_status()
    {
        var grant = CreateGrant(DiscountType.Percentage, 10);

        grant.Revoke();

        Assert.Equal(DiscountGrantStatus.Revoked, grant.Status);
    }
}

using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Domain.Tests.Entities.Billing;

public class InstallmentPlanTests
{
    private static InstallmentPlan CreatePlan(decimal total, decimal deposit) =>
        new(Guid.NewGuid(), Guid.NewGuid(), total, deposit, DateTimeOffset.UtcNow);

    [Fact]
    public void GenerateInstallments_creates_correct_count()
    {
        var plan = CreatePlan(1200m, 200m);

        plan.GenerateInstallments(4);

        Assert.Equal(4, plan.Installments.Count);
    }

    [Fact]
    public void GenerateInstallments_distributes_remaining_amount()
    {
        var plan = CreatePlan(1200m, 200m); // Remaining = 1000

        plan.GenerateInstallments(4); // 250 each

        Assert.Equal(1000m, plan.Installments.Sum(i => i.Amount));
    }

    [Fact]
    public void GenerateInstallments_handles_rounding_on_last_installment()
    {
        var plan = CreatePlan(1000m, 0m);

        plan.GenerateInstallments(3); // 333.33, 333.33, 333.34

        Assert.Equal(1000m, plan.Installments.Sum(i => i.Amount));
        // Last installment absorbs rounding remainder
        var amounts = plan.Installments.Select(i => i.Amount).ToList();
        Assert.True(amounts[2] >= amounts[1]); // Last >= others
    }

    [Fact]
    public void GenerateInstallments_sets_progressive_due_dates()
    {
        var startDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var plan = new InstallmentPlan(Guid.NewGuid(), Guid.NewGuid(), 600m, 0m, startDate);

        plan.GenerateInstallments(3);

        Assert.Equal(startDate.AddMonths(1), plan.Installments[0].DueDate);
        Assert.Equal(startDate.AddMonths(2), plan.Installments[1].DueDate);
        Assert.Equal(startDate.AddMonths(3), plan.Installments[2].DueDate);
    }

    [Fact]
    public void GenerateInstallments_throws_when_already_generated()
    {
        var plan = CreatePlan(600m, 0m);
        plan.GenerateInstallments(3);

        Assert.Throws<InvalidOperationException>(() => plan.GenerateInstallments(3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GenerateInstallments_throws_for_invalid_count(int count)
    {
        var plan = CreatePlan(600m, 0m);

        Assert.Throws<ArgumentException>(() => plan.GenerateInstallments(count));
    }

    [Fact]
    public void ApplyDepositDiscount_reduces_deposit()
    {
        var plan = CreatePlan(1000m, 200m);

        plan.ApplyDepositDiscount(50m);

        Assert.Equal(150m, plan.DepositAmount);
    }

    [Fact]
    public void ApplyDepositDiscount_caps_at_deposit_amount()
    {
        var plan = CreatePlan(1000m, 200m);

        plan.ApplyDepositDiscount(500m);

        Assert.Equal(0m, plan.DepositAmount);
    }
}

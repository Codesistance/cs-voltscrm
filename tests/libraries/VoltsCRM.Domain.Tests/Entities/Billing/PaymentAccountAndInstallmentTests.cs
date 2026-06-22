using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Tests.Entities.Billing;

public class PaymentAccountTests
{
    [Fact]
    public void New_account_has_zero_balance()
    {
        var account = new PaymentAccount(Guid.NewGuid());

        Assert.Equal(0m, account.Balance);
        Assert.Null(account.LastPaymentDate);
    }

    [Fact]
    public void Credit_increases_balance()
    {
        var account = new PaymentAccount(Guid.NewGuid());
        var paidAt = DateTimeOffset.UtcNow;

        account.Credit(500m, paidAt);

        Assert.Equal(500m, account.Balance);
        Assert.Equal(paidAt, account.LastPaymentDate);
    }

    [Fact]
    public void Multiple_credits_accumulate()
    {
        var account = new PaymentAccount(Guid.NewGuid());

        account.Credit(200m, DateTimeOffset.UtcNow);
        account.Credit(300m, DateTimeOffset.UtcNow);

        Assert.Equal(500m, account.Balance);
    }

    [Fact]
    public void Debit_decreases_balance()
    {
        var account = new PaymentAccount(Guid.NewGuid());
        account.Credit(500m, DateTimeOffset.UtcNow);

        account.Debit(200m);

        Assert.Equal(300m, account.Balance);
    }

    [Fact]
    public void Debit_can_go_negative()
    {
        var account = new PaymentAccount(Guid.NewGuid());
        account.Credit(100m, DateTimeOffset.UtcNow);

        account.Debit(300m);

        Assert.Equal(-200m, account.Balance);
    }
}

public class InstallmentTests
{
    private static Installment CreateInstallment(decimal amount = 500m) =>
        new(Guid.NewGuid(), amount, DateTimeOffset.UtcNow.AddDays(30));

    [Fact]
    public void New_installment_is_pending()
    {
        var installment = CreateInstallment();

        Assert.Equal(InstallmentStatus.Pending, installment.Status);
        Assert.Null(installment.PaidDate);
    }

    [Fact]
    public void MarkPaid_updates_status_and_date()
    {
        var installment = CreateInstallment();
        var paidAt = DateTimeOffset.UtcNow;

        installment.MarkPaid(paidAt);

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
        Assert.Equal(paidAt, installment.PaidDate);
    }

    [Fact]
    public void MarkOverdue_changes_pending_to_overdue()
    {
        var installment = CreateInstallment();

        installment.MarkOverdue();

        Assert.Equal(InstallmentStatus.Overdue, installment.Status);
    }

    [Fact]
    public void MarkOverdue_does_not_change_paid_status()
    {
        var installment = CreateInstallment();
        installment.MarkPaid(DateTimeOffset.UtcNow);

        installment.MarkOverdue();

        Assert.Equal(InstallmentStatus.Paid, installment.Status);
    }

    [Fact]
    public void UndoPayment_reverts_to_pending()
    {
        var installment = CreateInstallment();
        installment.MarkPaid(DateTimeOffset.UtcNow);

        installment.UndoPayment();

        Assert.Equal(InstallmentStatus.Pending, installment.Status);
        Assert.Null(installment.PaidDate);
    }
}

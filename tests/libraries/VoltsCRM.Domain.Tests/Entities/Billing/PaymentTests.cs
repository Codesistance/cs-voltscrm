using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Tests.Entities.Billing;

public class PaymentTests
{
    private static Payment CreatePayment(decimal amount = 1000m) =>
        new(Guid.NewGuid(), amount, "KES", PaymentMethod.Cash, PaymentChannel.Agent, DateTimeOffset.UtcNow);

    [Fact]
    public void AllocateToInvoice_adds_allocation()
    {
        var payment = CreatePayment();
        var invoiceId = Guid.NewGuid();

        payment.AllocateToInvoice(invoiceId, 500m);

        Assert.Single(payment.Allocations);
        Assert.Equal(invoiceId, payment.Allocations[0].InvoiceId);
        Assert.Equal(500m, payment.Allocations[0].Amount);
        Assert.Null(payment.Allocations[0].InstallmentId);
    }

    [Fact]
    public void AllocateToInstallment_adds_allocation()
    {
        var payment = CreatePayment();
        var installmentId = Guid.NewGuid();

        payment.AllocateToInstallment(installmentId, 300m);

        Assert.Single(payment.Allocations);
        Assert.Equal(installmentId, payment.Allocations[0].InstallmentId);
        Assert.Equal(300m, payment.Allocations[0].Amount);
        Assert.Null(payment.Allocations[0].InvoiceId);
    }

    [Fact]
    public void Multiple_allocations_are_tracked()
    {
        var payment = CreatePayment();

        payment.AllocateToInvoice(Guid.NewGuid(), 400m);
        payment.AllocateToInstallment(Guid.NewGuid(), 300m);
        payment.AllocateToInvoice(Guid.NewGuid(), 200m);

        Assert.Equal(3, payment.Allocations.Count);
        Assert.Equal(900m, payment.Allocations.Sum(a => a.Amount));
    }

    [Fact]
    public void ApplyDiscount_reduces_net_amount()
    {
        var payment = CreatePayment(1000m);
        var grantId = Guid.NewGuid();

        payment.ApplyDiscount(100m, grantId);

        Assert.Equal(100m, payment.DiscountApplied);
        Assert.Equal(900m, payment.NetAmount);
        Assert.Equal(grantId, payment.DiscountGrantId);
    }

    [Fact]
    public void ApplyDiscount_caps_at_payment_amount()
    {
        var payment = CreatePayment(500m);

        payment.ApplyDiscount(1000m, Guid.NewGuid());

        Assert.Equal(500m, payment.DiscountApplied);
        Assert.Equal(0m, payment.NetAmount);
    }

    [Fact]
    public void Lifecycle_transitions_work()
    {
        var payment = CreatePayment();
        Assert.Equal(PaymentStatus.Pending, payment.Status);

        payment.Complete();
        Assert.Equal(PaymentStatus.Completed, payment.Status);

        payment.Reverse();
        Assert.Equal(PaymentStatus.Reversed, payment.Status);
    }
}

using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Tests.Entities.Billing;

public class InvoiceTests
{
    private static Invoice CreateInvoice(decimal gross = 1000m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 2024, 6, gross, DateTimeOffset.UtcNow.AddDays(30));

    [Fact]
    public void RecordPayment_updates_amount_paid()
    {
        var invoice = CreateInvoice(1000m);

        invoice.RecordPayment(400m);

        Assert.Equal(400m, invoice.AmountPaid);
        Assert.Equal(600m, invoice.Balance);
    }

    [Fact]
    public void RecordPayment_sets_partially_paid_status()
    {
        var invoice = CreateInvoice(1000m);

        invoice.RecordPayment(500m);

        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
    }

    [Fact]
    public void RecordPayment_sets_paid_status_when_fully_paid()
    {
        var invoice = CreateInvoice(1000m);

        invoice.RecordPayment(1000m);

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(0m, invoice.Balance);
    }

    [Fact]
    public void RecordPayment_handles_overpayment()
    {
        var invoice = CreateInvoice(500m);

        invoice.RecordPayment(600m);

        Assert.Equal(600m, invoice.AmountPaid);
        Assert.Equal(-100m, invoice.Balance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void UndoPayment_decreases_amount_paid()
    {
        var invoice = CreateInvoice(1000m);
        invoice.RecordPayment(600m);

        invoice.UndoPayment(400m);

        Assert.Equal(200m, invoice.AmountPaid);
        Assert.Equal(800m, invoice.Balance);
    }

    [Fact]
    public void UndoPayment_reverts_to_pending_when_all_undone()
    {
        var invoice = CreateInvoice(1000m);
        invoice.RecordPayment(500m);
        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);

        invoice.UndoPayment(500m);

        Assert.Equal(InvoiceStatus.Pending, invoice.Status);
        Assert.Equal(0m, invoice.AmountPaid);
    }

    [Fact]
    public void UndoPayment_does_not_go_negative()
    {
        var invoice = CreateInvoice(1000m);
        invoice.RecordPayment(200m);

        invoice.UndoPayment(500m);

        Assert.Equal(0m, invoice.AmountPaid);
    }

    [Fact]
    public void UndoPayment_keeps_paid_status_when_still_fully_paid()
    {
        var invoice = CreateInvoice(500m);
        invoice.RecordPayment(600m); // Overpay by 100
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);

        invoice.UndoPayment(50m); // Still paid 550, AmountDue is 500

        Assert.Equal(550m, invoice.AmountPaid);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void MarkOverdue_changes_pending_to_overdue()
    {
        var invoice = CreateInvoice();

        invoice.MarkOverdue();

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public void MarkOverdue_does_not_change_non_pending_status()
    {
        var invoice = CreateInvoice();
        invoice.RecordPayment(invoice.AmountDue); // Paid

        invoice.MarkOverdue();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void AddLineItem_adds_to_collection()
    {
        var invoice = CreateInvoice();

        invoice.AddLineItem("Service charge", 500m);
        invoice.AddLineItem("Discount", -50m, isDiscount: true);

        Assert.Equal(2, invoice.LineItems.Count);
    }

    [Fact]
    public void AddLineItem_tracks_discount_amount()
    {
        var invoice = CreateInvoice(1000m);

        invoice.AddLineItem("Loyalty discount", -100m, isDiscount: true);
        invoice.AddLineItem("Promo discount", -50m, isDiscount: true);

        Assert.Equal(150m, invoice.DiscountAmount);
        Assert.Equal(850m, invoice.AmountDue);
    }
}

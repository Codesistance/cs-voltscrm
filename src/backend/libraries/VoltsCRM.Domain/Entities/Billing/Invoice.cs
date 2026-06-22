using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class Invoice : Entity
{
    public Guid CustomerSubscriptionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public int PeriodYear { get; private set; }
    public int PeriodMonth { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal AmountDue => GrossAmount - DiscountAmount;
    public decimal AmountPaid { get; private set; }
    public decimal Balance => AmountDue - AmountPaid;
    public DateTimeOffset DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;
    public string Currency { get; private set; } = Money.DefaultCurrency;

    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    private Invoice() { }

    public Invoice(Guid subscriptionId, Guid customerId, int year, int month,
        decimal grossAmount, DateTimeOffset dueDate, string currency = Money.DefaultCurrency)
    {
        CustomerSubscriptionId = subscriptionId;
        CustomerId = customerId;
        PeriodYear = year;
        PeriodMonth = month;
        GrossAmount = grossAmount;
        DueDate = dueDate;
        Currency = currency;
    }

    public void AddLineItem(string description, decimal amount, bool isDiscount = false)
    {
        _lineItems.Add(new InvoiceLineItem(Id, description, amount, isDiscount));
        if (isDiscount) DiscountAmount += Math.Abs(amount);
    }

    public void RecordPayment(decimal amount)
    {
        AmountPaid += amount;
        Status = AmountPaid >= AmountDue ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }

    public void UndoPayment(decimal amount)
    {
        AmountPaid = Math.Max(0, AmountPaid - amount);
        Status = AmountPaid >= AmountDue ? InvoiceStatus.Paid
               : AmountPaid <= 0 ? InvoiceStatus.Pending
               : InvoiceStatus.PartiallyPaid;
    }

    public void MarkOverdue() { if (Status == InvoiceStatus.Pending) Status = InvoiceStatus.Overdue; }
}

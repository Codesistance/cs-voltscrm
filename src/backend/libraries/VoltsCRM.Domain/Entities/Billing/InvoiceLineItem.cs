using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Billing;

public class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public bool IsDiscount { get; private set; }

    private InvoiceLineItem() { }

    public InvoiceLineItem(Guid invoiceId, string description, decimal amount, bool isDiscount = false)
    {
        InvoiceId = invoiceId;
        Description = description;
        Amount = amount;
        IsDiscount = isDiscount;
    }
}

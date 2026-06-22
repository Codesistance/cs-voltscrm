using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Billing;

public class PaymentAllocation : Entity
{
    public Guid PaymentId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? InstallmentId { get; private set; }
    public decimal Amount { get; private set; }

    private PaymentAllocation() { }

    public PaymentAllocation(Guid paymentId, Guid? invoiceId, Guid? installmentId, decimal amount)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        InstallmentId = installmentId;
        Amount = amount;
    }
}

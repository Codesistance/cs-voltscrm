using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class Payment : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal DiscountApplied { get; private set; }
    public decimal NetAmount => Amount - DiscountApplied;
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public PaymentMethod Method { get; private set; }
    public PaymentChannel Channel { get; private set; }
    public string? PlatformProvider { get; private set; }
    public string? PlatformReference { get; private set; }
    public string? CollectedByAgentId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTimeOffset PaymentDate { get; private set; }
    public Guid? DiscountGrantId { get; private set; }

    private readonly List<PaymentAllocation> _allocations = [];
    public IReadOnlyList<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    private Payment() { }

    public Payment(Guid customerId, decimal amount, string currency, PaymentMethod method,
        PaymentChannel channel, DateTimeOffset paymentDate,
        string? platformProvider = null, string? platformReference = null,
        string? collectedByAgentId = null)
    {
        CustomerId = customerId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Channel = channel;
        PaymentDate = paymentDate;
        PlatformProvider = platformProvider;
        PlatformReference = platformReference;
        CollectedByAgentId = collectedByAgentId;
    }

    public void ApplyDiscount(decimal discountAmount, Guid grantId)
    {
        DiscountApplied = Math.Min(discountAmount, Amount);
        DiscountGrantId = grantId;
    }

    public void Complete() => Status = PaymentStatus.Completed;
    public void Fail() => Status = PaymentStatus.Failed;
    public void Reverse() => Status = PaymentStatus.Reversed;

    public void AllocateToInvoice(Guid invoiceId, decimal amount)
        => _allocations.Add(new PaymentAllocation(Id, invoiceId, null, amount));

    public void AllocateToInstallment(Guid installmentId, decimal amount)
        => _allocations.Add(new PaymentAllocation(Id, null, installmentId, amount));
}

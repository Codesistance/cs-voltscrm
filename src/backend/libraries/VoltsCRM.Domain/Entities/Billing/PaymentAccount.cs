using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Billing;

public class PaymentAccount : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public DateTimeOffset? LastPaymentDate { get; private set; }
    public uint RowVersion { get; private set; }

    private PaymentAccount() { }

    public PaymentAccount(Guid customerId, string currency = Money.DefaultCurrency)
    {
        CustomerId = customerId;
        Currency = currency;
        Balance = 0m;
    }

    public void Credit(decimal amount, DateTimeOffset paidAt)
    {
        Balance += amount;
        LastPaymentDate = paidAt;
    }

    public void Debit(decimal amount) => Balance -= amount;
}

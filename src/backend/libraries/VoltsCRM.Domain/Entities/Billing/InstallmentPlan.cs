using VoltsCRM.Domain.Common;

namespace VoltsCRM.Domain.Entities.Billing;

public class InstallmentPlan : Entity
{
    public Guid CustomerSubscriptionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal DepositAmount { get; private set; }
    public decimal RemainingAmount => TotalAmount - DepositAmount;
    public string Currency { get; private set; } = Money.DefaultCurrency;
    public DateTimeOffset StartDate { get; private set; }

    private readonly List<Installment> _installments = [];
    public IReadOnlyList<Installment> Installments => _installments.AsReadOnly();

    private InstallmentPlan() { }

    public InstallmentPlan(Guid subscriptionId, Guid customerId, decimal totalAmount,
        decimal depositAmount, DateTimeOffset startDate, string currency = "KES")
    {
        CustomerSubscriptionId = subscriptionId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        DepositAmount = depositAmount;
        StartDate = startDate;
        Currency = currency;
    }

    public void GenerateInstallments(int count)
    {
        if (count <= 0) throw new ArgumentException("Count must be positive.", nameof(count));
        if (_installments.Count > 0) throw new InvalidOperationException("Installments have already been generated.");

        var monthly = Math.Round(RemainingAmount / count, 2);
        var subtotal = monthly * (count - 1);

        for (int i = 0; i < count - 1; i++)
            _installments.Add(new Installment(Id, monthly, StartDate.AddMonths(i + 1)));

        // Last installment absorbs any rounding remainder so the total is exact
        _installments.Add(new Installment(Id, RemainingAmount - subtotal, StartDate.AddMonths(count)));
    }

    public void ApplyDepositDiscount(decimal discountAmount)
    {
        if (discountAmount > DepositAmount) discountAmount = DepositAmount;
        DepositAmount -= discountAmount;
    }
}

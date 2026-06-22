using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class Installment : Entity
{
    public Guid InstallmentPlanId { get; private set; }
    public InstallmentPlan InstallmentPlan { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public DateTimeOffset DueDate { get; private set; }
    public DateTimeOffset? PaidDate { get; private set; }
    public InstallmentStatus Status { get; private set; } = InstallmentStatus.Pending;

    private Installment() { }

    public Installment(Guid planId, decimal amount, DateTimeOffset dueDate)
    {
        InstallmentPlanId = planId;
        Amount = amount;
        DueDate = dueDate;
    }

    public void MarkPaid(DateTimeOffset paidAt)
    {
        Status = InstallmentStatus.Paid;
        PaidDate = paidAt;
    }

    public void MarkOverdue() { if (Status == InstallmentStatus.Pending) Status = InstallmentStatus.Overdue; }

    public void UndoPayment()
    {
        Status = InstallmentStatus.Pending;
        PaidDate = null;
    }
}

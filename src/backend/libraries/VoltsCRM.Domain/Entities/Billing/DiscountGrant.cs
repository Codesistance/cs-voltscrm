using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Entities.Billing;

public class DiscountGrant : Entity
{
    public Guid CustomerId { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public DiscountScope Scope { get; private set; }
    public Guid? TargetId { get; private set; }
    public bool IsRecurring { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public string GrantedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset GrantedAt { get; private set; }
    public string? Reason { get; private set; }
    public DiscountGrantStatus Status { get; private set; } = DiscountGrantStatus.Active;

    private DiscountGrant() { }

    public DiscountGrant(Guid customerId, DiscountType discountType, decimal value, DiscountScope scope,
        string grantedByUserId, bool isRecurring = false, Guid? targetId = null,
        DateTimeOffset? validFrom = null, DateTimeOffset? validUntil = null, string? reason = null)
    {
        CustomerId = customerId;
        DiscountType = discountType;
        Value = value;
        Scope = scope;
        GrantedByUserId = grantedByUserId;
        IsRecurring = isRecurring;
        TargetId = targetId;
        ValidFrom = validFrom ?? DateTimeOffset.UtcNow;
        ValidUntil = validUntil;
        Reason = reason;
        GrantedAt = DateTimeOffset.UtcNow;
    }

    public decimal CalculateDiscountAmount(decimal grossAmount)
    {
        return DiscountType switch
        {
            DiscountType.Percentage => Math.Round(grossAmount * Value / 100m, 2),
            DiscountType.FixedAmount => Math.Min(Value, grossAmount),
            _ => throw new InvalidOperationException($"Unhandled discount type: {DiscountType}")
        };
    }

    public bool IsActiveAt(DateTimeOffset at) =>
        Status == DiscountGrantStatus.Active && at >= ValidFrom && (ValidUntil is null || at <= ValidUntil);

    public void Revoke() => Status = DiscountGrantStatus.Revoked;
    public void MarkApplied() { if (!IsRecurring) Status = DiscountGrantStatus.Applied; }
    public void Expire() => Status = DiscountGrantStatus.Expired;
}

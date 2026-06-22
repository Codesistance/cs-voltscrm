namespace VoltsCRM.Application.Features.Discounts;

public sealed record DiscountGrantDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string DiscountType,
    decimal Value,
    string Scope,
    Guid? TargetId,
    bool IsRecurring,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string GrantedByUserId,
    DateTimeOffset GrantedAt,
    string? Reason,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

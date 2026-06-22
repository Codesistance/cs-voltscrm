using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record SubscriptionListItemDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerAccountNumber,
    Guid ServicePlanId,
    string PlanName,
    string BillingType,
    string Status,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate);

public sealed record DeployedItemDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemCode,
    string InventoryItemName,
    string? SerialNumber,
    Guid StockMovementId,
    DateTimeOffset DispatchedDate);

public sealed record SubscriptionDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerAccountNumber,
    Guid ServicePlanId,
    string PlanCode,
    string PlanName,
    string BillingType,
    string Status,
    MoneyDto? NegotiatedPrice,
    MoneyDto EffectivePrice,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    IReadOnlyList<DeployedItemDto> DeployedItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

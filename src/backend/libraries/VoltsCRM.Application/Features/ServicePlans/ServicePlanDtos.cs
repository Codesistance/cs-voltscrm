using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record ServicePlanListItemDto(
    Guid Id,
    string PlanCode,
    string Name,
    string BillingType,
    string BillingCycle,
    MoneyDto BasePrice,
    string Status,
    int LineItemCount);

public sealed record ServicePlanDto(
    Guid Id,
    string PlanCode,
    string Name,
    string? Description,
    string BillingType,
    string BillingCycle,
    MoneyDto BasePrice,
    string Status,
    IReadOnlyList<ServicePlanLineItemDto> LineItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ServicePlanLineItemDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemCode,
    string InventoryItemName,
    int Quantity,
    string Role);

/// <summary>Line item supplied by the client when creating/updating a plan.</summary>
public sealed record ServicePlanLineItemInput(Guid InventoryItemId, int Quantity, string Role);

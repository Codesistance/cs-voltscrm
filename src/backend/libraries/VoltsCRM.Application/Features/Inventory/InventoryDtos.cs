using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record InventoryItemDto(
    Guid Id,
    string ItemCode,
    string Name,
    string? Description,
    Guid CategoryId,
    string CategoryName,
    bool CategoryTracksStock,
    string UnitOfMeasure,
    int? QuantityOnHand,
    int? ReorderLevel,
    MoneyDto UnitCost,
    bool IsActive,
    bool IsLowStock,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record InventoryItemListItemDto(
    Guid Id,
    string ItemCode,
    string Name,
    Guid CategoryId,
    string CategoryName,
    string UnitOfMeasure,
    int? QuantityOnHand,
    int? ReorderLevel,
    MoneyDto UnitCost,
    bool IsLowStock);

public sealed record StockMovementDto(
    Guid Id,
    Guid InventoryItemId,
    string MovementType,
    int Quantity,
    string? Reference,
    string? Notes,
    string? MovedByUserId,
    DateTimeOffset CreatedAt);

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record InventoryCategoryDto(
    Guid Id,
    string Name,
    string? Code,
    bool TracksStock,
    bool IsActive,
    int ItemCount);

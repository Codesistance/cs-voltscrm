using MediatR;
using System.Security.Claims;
using VoltsCRM.API.Auth;
using VoltsCRM.Application.Authorization;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Features.Inventory;
using VoltsCRM.Application.Features.Inventory.Categories;

namespace VoltsCRM.API.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // Inventory is an Administration-area resource. Each endpoint declares its own permission
        // (policies AND together, so gating per-endpoint avoids requiring view+manage simultaneously).
        var group = app.MapGroup("/api/inventory").WithTags("Inventory");

        // Categories (admin-managed). List/get are readable by anyone who can view inventory (so the
        // item form can populate its dropdown); mutations require the dedicated permission.
        group.MapGet("/categories", ListCategoriesAsync).RequirePermission(Permissions.InventoryView);
        group.MapGet("/categories/{id:guid}", GetCategoryAsync).RequirePermission(Permissions.InventoryView);
        group.MapPost("/categories", CreateCategoryAsync).RequirePermission(Permissions.InventoryCategoriesManage);
        group.MapPut("/categories/{id:guid}", UpdateCategoryAsync).RequirePermission(Permissions.InventoryCategoriesManage);
        group.MapPost("/categories/{id:guid}/deactivate", DeactivateCategoryAsync).RequirePermission(Permissions.InventoryCategoriesManage);

        group.MapGet("", ListAsync).RequirePermission(Permissions.InventoryView);
        group.MapGet("/{id:guid}", GetAsync).RequirePermission(Permissions.InventoryView);
        group.MapPost("", CreateAsync).RequirePermission(Permissions.InventoryManage);
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission(Permissions.InventoryManage);
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync).RequirePermission(Permissions.InventoryManage);
        group.MapGet("/{id:guid}/movements", ListMovementsAsync).RequirePermission(Permissions.InventoryView);
        group.MapPost("/{id:guid}/stock-movements", RecordMovementAsync).RequirePermission(Permissions.InventoryManage);

        return app;
    }

    private static async Task<IResult> ListCategoriesAsync(ISender sender, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListInventoryCategoriesQuery(), ct));

    private static async Task<IResult> GetCategoryAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetInventoryCategoryQuery(id), ct));

    private static async Task<IResult> CreateCategoryAsync(
        ISender sender, CreateInventoryCategoryCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/inventory/categories/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateCategoryAsync(
        ISender sender, Guid id, UpdateInventoryCategoryRequest req, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new UpdateInventoryCategoryCommand(id, req.Name, req.Code, req.TracksStock), ct));

    private static async Task<IResult> DeactivateCategoryAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new DeactivateInventoryCategoryCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListAsync(
        ISender sender, int? page, int? pageSize, string? q, Guid? categoryId, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListInventoryQuery(page ?? 1, pageSize ?? 20, q, categoryId), ct));

    private static async Task<IResult> GetAsync(ISender sender, Guid id, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new GetInventoryItemQuery(id), ct));

    private static async Task<IResult> CreateAsync(
        ISender sender, CreateInventoryItemCommand command, CancellationToken ct)
    {
        var dto = await sender.Send(command, ct);
        return TypedResults.Created($"/api/inventory/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateAsync(
        ISender sender, Guid id, UpdateInventoryItemRequest req, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(
            new UpdateInventoryItemCommand(id, req.Name, req.Description, req.UnitCost, req.ReorderLevel), ct));

    private static async Task<IResult> DeactivateAsync(ISender sender, Guid id, CancellationToken ct)
    {
        await sender.Send(new DeactivateInventoryItemCommand(id), ct);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ListMovementsAsync(
        ISender sender, Guid id, int? page, int? pageSize, CancellationToken ct)
        => TypedResults.Ok(await sender.Send(new ListStockMovementsQuery(id, page ?? 1, pageSize ?? 20), ct));

    private static async Task<IResult> RecordMovementAsync(
        ISender sender, Guid id, RecordStockMovementRequest req, ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var dto = await sender.Send(
            new RecordStockMovementCommand(id, req.MovementType, req.Quantity, req.Reference, req.Notes, userId), ct);
        return TypedResults.Ok(dto);
    }
}

public sealed record UpdateInventoryItemRequest(string Name, string? Description, MoneyDto UnitCost, int? ReorderLevel);

public sealed record UpdateInventoryCategoryRequest(string Name, string? Code, bool TracksStock);

public sealed record RecordStockMovementRequest(string MovementType, int Quantity, string? Reference, string? Notes);

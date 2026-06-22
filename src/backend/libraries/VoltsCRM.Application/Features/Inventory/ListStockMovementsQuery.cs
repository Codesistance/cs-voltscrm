using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record ListStockMovementsQuery(Guid InventoryItemId, int Page, int PageSize)
    : IRequest<PagedResult<StockMovementDto>>;

public sealed class ListStockMovementsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListStockMovementsQuery, PagedResult<StockMovementDto>>
{
    public async Task<PagedResult<StockMovementDto>> Handle(ListStockMovementsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var q = db.StockMovements.AsNoTracking().Where(s => s.InventoryItemId == query.InventoryItemId);

        var total = await q.CountAsync(ct);
        var movements = await q.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<StockMovementDto>(
            movements.Select(s => s.ToDto()).ToList(), page, size, total);
    }
}

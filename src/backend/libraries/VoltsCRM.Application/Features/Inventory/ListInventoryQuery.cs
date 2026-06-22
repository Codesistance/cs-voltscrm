using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record ListInventoryQuery(int Page, int PageSize, string? Q, Guid? CategoryId)
    : IRequest<PagedResult<InventoryItemListItemDto>>;

public sealed class ListInventoryQueryHandler(IAppDbContext db)
    : IRequestHandler<ListInventoryQuery, PagedResult<InventoryItemListItemDto>>
{
    public async Task<PagedResult<InventoryItemListItemDto>> Handle(ListInventoryQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var q = db.InventoryItems.AsNoTracking().Include(i => i.Category).AsQueryable();

        if (query.CategoryId is { } categoryId)
        {
            q = q.Where(i => i.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            q = q.Where(i => i.ItemCode.ToLower().Contains(term) || i.Name.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(i => i.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<InventoryItemListItemDto>(
            items.Select(i => i.ToListItem()).ToList(), page, size, total);
    }
}

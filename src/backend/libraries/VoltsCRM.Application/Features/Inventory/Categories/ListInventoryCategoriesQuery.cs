using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record ListInventoryCategoriesQuery : IRequest<IReadOnlyList<InventoryCategoryDto>>;

public sealed class ListInventoryCategoriesQueryHandler(IAppDbContext db)
    : IRequestHandler<ListInventoryCategoriesQuery, IReadOnlyList<InventoryCategoryDto>>
{
    public async Task<IReadOnlyList<InventoryCategoryDto>> Handle(ListInventoryCategoriesQuery query, CancellationToken ct)
    {
        var categories = await db.InventoryCategories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new InventoryCategoryDto(
                c.Id, c.Name, c.Code, c.TracksStock, c.IsActive,
                db.InventoryItems.Count(i => i.CategoryId == c.Id)))
            .ToListAsync(ct);

        return categories;
    }
}

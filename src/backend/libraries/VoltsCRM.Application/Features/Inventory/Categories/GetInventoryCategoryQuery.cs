using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record GetInventoryCategoryQuery(Guid Id) : IRequest<InventoryCategoryDto>;

public sealed class GetInventoryCategoryQueryHandler(IAppDbContext db)
    : IRequestHandler<GetInventoryCategoryQuery, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(GetInventoryCategoryQuery query, CancellationToken ct)
    {
        var category = await db.InventoryCategories.AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new InventoryCategoryDto(
                c.Id, c.Name, c.Code, c.TracksStock, c.IsActive,
                db.InventoryItems.Count(i => i.CategoryId == c.Id)))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(InventoryCategory), query.Id);

        return category;
    }
}

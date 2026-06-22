using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record GetInventoryItemQuery(Guid Id) : IRequest<InventoryItemDto>;

public sealed class GetInventoryItemQueryHandler(IAppDbContext db)
    : IRequestHandler<GetInventoryItemQuery, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(GetInventoryItemQuery query, CancellationToken ct)
    {
        var item = await db.InventoryItems.AsNoTracking()
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), query.Id);
        return item.ToDto();
    }
}

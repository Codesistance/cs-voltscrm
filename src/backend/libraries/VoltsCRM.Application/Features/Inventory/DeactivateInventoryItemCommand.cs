using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record DeactivateInventoryItemCommand(Guid Id) : IRequest;

public sealed class DeactivateInventoryItemHandler(IAppDbContext db)
    : IRequestHandler<DeactivateInventoryItemCommand>
{
    public async Task Handle(DeactivateInventoryItemCommand cmd, CancellationToken ct)
    {
        var item = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), cmd.Id);
        item.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}

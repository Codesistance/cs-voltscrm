using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record DeactivateInventoryCategoryCommand(Guid Id) : IRequest;

public sealed class DeactivateInventoryCategoryHandler(IAppDbContext db)
    : IRequestHandler<DeactivateInventoryCategoryCommand>
{
    public async Task Handle(DeactivateInventoryCategoryCommand cmd, CancellationToken ct)
    {
        var category = await db.InventoryCategories.FirstOrDefaultAsync(c => c.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(InventoryCategory), cmd.Id);

        if (await db.InventoryItems.AnyAsync(i => i.CategoryId == category.Id, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(DeactivateInventoryCategoryCommand.Id),
                    "Cannot deactivate a category that still has inventory items."),
            });

        category.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record UpdateInventoryItemCommand(
    Guid Id,
    string Name,
    string? Description,
    MoneyDto UnitCost,
    int? ReorderLevel) : IRequest<InventoryItemDto>;

public sealed class UpdateInventoryItemValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitCost).NotNull();
        RuleFor(x => x.UnitCost.Amount).GreaterThanOrEqualTo(0).When(x => x.UnitCost is not null);
        RuleFor(x => x.UnitCost.Currency).NotEmpty().Length(3).When(x => x.UnitCost is not null);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).When(x => x.ReorderLevel.HasValue);
    }
}

public sealed class UpdateInventoryItemHandler(IAppDbContext db)
    : IRequestHandler<UpdateInventoryItemCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(UpdateInventoryItemCommand cmd, CancellationToken ct)
    {
        var item = await db.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), cmd.Id);

        var tracksStock = item.Category?.TracksStock ?? true;
        item.Update(cmd.Name, cmd.Description, new Money(cmd.UnitCost.Amount, cmd.UnitCost.Currency),
            cmd.ReorderLevel, tracksStock);
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }
}

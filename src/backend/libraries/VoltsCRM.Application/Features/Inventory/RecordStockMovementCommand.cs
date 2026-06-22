using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record RecordStockMovementCommand(
    Guid InventoryItemId,
    string MovementType,
    int Quantity,
    string? Reference,
    string? Notes,
    string? MovedByUserId) : IRequest<StockMovementDto>;

public sealed class RecordStockMovementValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementValidator()
    {
        RuleFor(x => x.MovementType).NotEmpty()
            .Must(t => Enum.TryParse<StockMovementType>(t, true, out _)).WithMessage("Invalid movement type.");
        RuleFor(x => x.Quantity).NotEqual(0).WithMessage("Quantity must be non-zero.");
        RuleFor(x => x.Quantity).GreaterThan(0)
            .When(x => !string.Equals(x.MovementType, nameof(StockMovementType.Adjustment), StringComparison.OrdinalIgnoreCase))
            .WithMessage("Quantity must be positive.");
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RecordStockMovementHandler(IAppDbContext db)
    : IRequestHandler<RecordStockMovementCommand, StockMovementDto>
{
    public async Task<StockMovementDto> Handle(RecordStockMovementCommand cmd, CancellationToken ct)
    {
        var type = Enum.Parse<StockMovementType>(cmd.MovementType, true);

        var item = await db.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == cmd.InventoryItemId, ct)
            ?? throw new NotFoundException(nameof(InventoryItem), cmd.InventoryItemId);

        var tracksStock = item.Category?.TracksStock ?? true;
        if (!tracksStock)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RecordStockMovementCommand.MovementType),
                    "Stock movements don't apply to items in a non-stock-tracked category."),
            });

        // In/Adjustment add stock; Out/Dispatched/Transfer remove it. Adjustment may be signed.
        var delta = type switch
        {
            StockMovementType.In => cmd.Quantity,
            StockMovementType.Adjustment => cmd.Quantity,
            _ => -cmd.Quantity,
        };
        item.AdjustStock(delta, tracksStock);

        var movement = new StockMovement(item.Id, type, cmd.Quantity, cmd.Reference,
            movedByUserId: cmd.MovedByUserId, notes: cmd.Notes);
        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        return movement.ToDto();
    }
}

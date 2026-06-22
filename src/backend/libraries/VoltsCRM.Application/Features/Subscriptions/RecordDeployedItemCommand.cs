using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Inventory;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record RecordDeployedItemCommand(
    Guid SubscriptionId,
    Guid InventoryItemId,
    int Quantity,
    string? SerialNumber,
    string? MovedByUserId) : IRequest<DeployedItemDto>;

public sealed class RecordDeployedItemValidator : AbstractValidator<RecordDeployedItemCommand>
{
    public RecordDeployedItemValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
    }
}

public sealed class RecordDeployedItemHandler(IAppDbContext db) : IRequestHandler<RecordDeployedItemCommand, DeployedItemDto>
{
    public async Task<DeployedItemDto> Handle(RecordDeployedItemCommand cmd, CancellationToken ct)
    {
        var sub = await db.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == cmd.SubscriptionId, ct)
            ?? throw new NotFoundException(nameof(CustomerSubscription), cmd.SubscriptionId);

        if (sub.Status == SubscriptionStatus.Terminated)
            throw new ValidationException(new[]
            {
                new ValidationFailure("status", "Cannot dispatch to a terminated subscription."),
            });

        var item = await db.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == cmd.InventoryItemId, ct);
        if (item is null)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RecordDeployedItemCommand.InventoryItemId), "Inventory item does not exist."),
            });

        var tracksStock = item.Category?.TracksStock ?? true;
        if (!tracksStock)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RecordDeployedItemCommand.InventoryItemId),
                    "Items in a non-stock-tracked category cannot be dispatched."),
            });

        var movement = new StockMovement(
            item.Id, StockMovementType.Dispatched, cmd.Quantity,
            reference: $"Dispatch for subscription {sub.Id}",
            relatedSubscriptionId: sub.Id,
            movedByUserId: cmd.MovedByUserId);
        item.AdjustStock(-cmd.Quantity, tracksStock);
        db.StockMovements.Add(movement);

        var deployed = sub.RecordDispatchedItem(item.Id, movement.Id, cmd.SerialNumber);
        db.DeployedItems.Add(deployed); // explicit Add → INSERT (app-generated key)

        await db.SaveChangesAsync(ct);

        return new DeployedItemDto(deployed.Id, item.Id, item.ItemCode, item.Name,
            deployed.SerialNumber, movement.Id, deployed.DispatchedDate);
    }
}

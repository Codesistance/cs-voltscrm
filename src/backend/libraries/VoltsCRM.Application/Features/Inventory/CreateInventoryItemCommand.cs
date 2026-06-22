using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory;

public sealed record CreateInventoryItemCommand(
    string ItemCode,
    string Name,
    string? Description,
    Guid CategoryId,
    string UnitOfMeasure,
    MoneyDto UnitCost,
    int? QuantityOnHand,
    int? ReorderLevel) : IRequest<InventoryItemDto>;

public sealed class CreateInventoryItemValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemValidator()
    {
        RuleFor(x => x.ItemCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(30);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.UnitCost).NotNull();
        RuleFor(x => x.UnitCost.Amount).GreaterThanOrEqualTo(0).When(x => x.UnitCost is not null);
        RuleFor(x => x.UnitCost.Currency).NotEmpty().Length(3).When(x => x.UnitCost is not null);
        RuleFor(x => x.QuantityOnHand).GreaterThanOrEqualTo(0).When(x => x.QuantityOnHand.HasValue);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0).When(x => x.ReorderLevel.HasValue);
    }
}

public sealed class CreateInventoryItemHandler(IAppDbContext db)
    : IRequestHandler<CreateInventoryItemCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(CreateInventoryItemCommand cmd, CancellationToken ct)
    {
        var category = await db.InventoryCategories.FirstOrDefaultAsync(c => c.Id == cmd.CategoryId, ct);
        if (category is null || !category.IsActive)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateInventoryItemCommand.CategoryId),
                    "Category does not exist or is inactive."),
            });

        var codeTaken = await db.InventoryItems.IgnoreQueryFilters()
            .AnyAsync(i => i.ItemCode == cmd.ItemCode, ct);
        if (codeTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateInventoryItemCommand.ItemCode),
                    $"Item code '{cmd.ItemCode}' is already in use."),
            });

        var item = new InventoryItem(
            cmd.ItemCode, cmd.Name, category, new Money(cmd.UnitCost.Amount, cmd.UnitCost.Currency),
            cmd.UnitOfMeasure, cmd.Description, cmd.QuantityOnHand, cmd.ReorderLevel);

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item.ToDto();
    }
}

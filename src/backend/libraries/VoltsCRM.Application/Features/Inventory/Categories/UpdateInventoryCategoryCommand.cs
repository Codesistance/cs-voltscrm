using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record UpdateInventoryCategoryCommand(
    Guid Id,
    string Name,
    string? Code,
    bool TracksStock) : IRequest<InventoryCategoryDto>;

public sealed class UpdateInventoryCategoryValidator : AbstractValidator<UpdateInventoryCategoryCommand>
{
    public UpdateInventoryCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).MaximumLength(30);
    }
}

public sealed class UpdateInventoryCategoryHandler(IAppDbContext db)
    : IRequestHandler<UpdateInventoryCategoryCommand, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(UpdateInventoryCategoryCommand cmd, CancellationToken ct)
    {
        var category = await db.InventoryCategories.FirstOrDefaultAsync(c => c.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(InventoryCategory), cmd.Id);

        var name = cmd.Name.Trim();
        if (await db.InventoryCategories.AnyAsync(c => c.Name == name && c.Id != cmd.Id, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateInventoryCategoryCommand.Name),
                    $"A category named '{name}' already exists."),
            });

        category.Update(name, cmd.Code, cmd.TracksStock);
        await db.SaveChangesAsync(ct);

        var itemCount = await db.InventoryItems.CountAsync(i => i.CategoryId == category.Id, ct);
        return new InventoryCategoryDto(category.Id, category.Name, category.Code,
            category.TracksStock, category.IsActive, itemCount);
    }
}

using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Inventory;

namespace VoltsCRM.Application.Features.Inventory.Categories;

public sealed record CreateInventoryCategoryCommand(
    string Name,
    string? Code,
    bool TracksStock) : IRequest<InventoryCategoryDto>;

public sealed class CreateInventoryCategoryValidator : AbstractValidator<CreateInventoryCategoryCommand>
{
    public CreateInventoryCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).MaximumLength(30);
    }
}

public sealed class CreateInventoryCategoryHandler(IAppDbContext db)
    : IRequestHandler<CreateInventoryCategoryCommand, InventoryCategoryDto>
{
    public async Task<InventoryCategoryDto> Handle(CreateInventoryCategoryCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await db.InventoryCategories.AnyAsync(c => c.Name == name, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateInventoryCategoryCommand.Name),
                    $"A category named '{name}' already exists."),
            });

        var category = new InventoryCategory(name, cmd.TracksStock, cmd.Code);
        db.InventoryCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return new InventoryCategoryDto(category.Id, category.Name, category.Code,
            category.TracksStock, category.IsActive, 0);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record UpdateServicePlanCommand(
    Guid Id,
    string Name,
    string? Description,
    MoneyDto BasePrice,
    IReadOnlyList<ServicePlanLineItemInput> LineItems) : IRequest<ServicePlanDto>;

public sealed class UpdateServicePlanValidator : AbstractValidator<UpdateServicePlanCommand>
{
    public UpdateServicePlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BasePrice).NotNull();
        RuleFor(x => x.BasePrice.Amount).GreaterThanOrEqualTo(0).When(x => x.BasePrice is not null);
        RuleFor(x => x.BasePrice.Currency).NotEmpty().Length(3).When(x => x.BasePrice is not null);
        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            li.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be positive.");
            li.RuleFor(l => l.Role).Must(r => Enum.TryParse<PlanLineItemRole>(r, true, out _)).WithMessage("Invalid role.");
        });
    }
}

public sealed class UpdateServicePlanHandler(IAppDbContext db)
    : IRequestHandler<UpdateServicePlanCommand, ServicePlanDto>
{
    public async Task<ServicePlanDto> Handle(UpdateServicePlanCommand cmd, CancellationToken ct)
    {
        var plan = await db.ServicePlans
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(ServicePlan), cmd.Id);

        await PlanLineItemHelper.EnsureItemsExistAsync(db, cmd.LineItems, ct);

        plan.Update(cmd.Name, cmd.Description);
        plan.UpdatePrice(new Money(cmd.BasePrice.Amount, cmd.BasePrice.Currency));

        // Replace the full line-item set (the form submits the complete list).
        foreach (var existing in plan.LineItems.ToList())
            plan.RemoveLineItem(existing.Id);

        foreach (var li in cmd.LineItems)
            plan.AddLineItem(li.InventoryItemId, li.Quantity, Enum.Parse<PlanLineItemRole>(li.Role, true));

        await db.SaveChangesAsync(ct);
        return await plan.ToDetailDtoAsync(db, ct);
    }
}

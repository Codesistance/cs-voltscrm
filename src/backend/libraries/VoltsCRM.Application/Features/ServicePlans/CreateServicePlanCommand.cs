using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record CreateServicePlanCommand(
    string PlanCode,
    string Name,
    string? Description,
    string BillingType,
    string BillingCycle,
    MoneyDto BasePrice,
    IReadOnlyList<ServicePlanLineItemInput> LineItems) : IRequest<ServicePlanDto>;

public sealed class CreateServicePlanValidator : AbstractValidator<CreateServicePlanCommand>
{
    public CreateServicePlanValidator()
    {
        RuleFor(x => x.PlanCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BillingType).Must(t => Enum.TryParse<BillingType>(t, true, out _)).WithMessage("Invalid billing type.");
        RuleFor(x => x.BillingCycle).Must(c => Enum.TryParse<BillingCycle>(c, true, out _)).WithMessage("Invalid billing cycle.");
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

public sealed class CreateServicePlanHandler(IAppDbContext db)
    : IRequestHandler<CreateServicePlanCommand, ServicePlanDto>
{
    public async Task<ServicePlanDto> Handle(CreateServicePlanCommand cmd, CancellationToken ct)
    {
        var billingType = Enum.Parse<BillingType>(cmd.BillingType, true);
        var billingCycle = Enum.Parse<BillingCycle>(cmd.BillingCycle, true);

        var codeTaken = await db.ServicePlans.AnyAsync(p => p.PlanCode == cmd.PlanCode, ct);
        if (codeTaken)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateServicePlanCommand.PlanCode),
                    $"Plan code '{cmd.PlanCode}' is already in use."),
            });

        await PlanLineItemHelper.EnsureItemsExistAsync(db, cmd.LineItems, ct);

        var plan = new ServicePlan(
            cmd.PlanCode, cmd.Name, billingType, billingCycle,
            new Money(cmd.BasePrice.Amount, cmd.BasePrice.Currency), cmd.Description);

        foreach (var li in cmd.LineItems)
            plan.AddLineItem(li.InventoryItemId, li.Quantity, Enum.Parse<PlanLineItemRole>(li.Role, true));

        db.ServicePlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return await plan.ToDetailDtoAsync(db, ct);
    }
}

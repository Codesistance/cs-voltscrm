using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Installments;

public sealed record CreateInstallmentPlanCommand(
    Guid SubscriptionId,
    decimal TotalAmount,
    decimal DepositAmount,
    int InstallmentCount,
    DateTimeOffset StartDate) : IRequest<InstallmentPlanDto>;

public sealed class CreateInstallmentPlanCommandValidator : AbstractValidator<CreateInstallmentPlanCommand>
{
    public CreateInstallmentPlanCommandValidator()
    {
        RuleFor(x => x.SubscriptionId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0m);
        RuleFor(x => x.DepositAmount).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.InstallmentCount).InclusiveBetween(1, 120);
        RuleFor(x => x).Must(x => x.DepositAmount <= x.TotalAmount)
            .WithMessage("DepositAmount cannot exceed TotalAmount.");
    }
}

public sealed class CreateInstallmentPlanCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateInstallmentPlanCommand, InstallmentPlanDto>
{
    public async Task<InstallmentPlanDto> Handle(CreateInstallmentPlanCommand cmd, CancellationToken ct)
    {
        var subscription = await db.CustomerSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == cmd.SubscriptionId, ct)
            ?? throw new NotFoundException(nameof(CustomerSubscription), cmd.SubscriptionId);

        var plan = new InstallmentPlan(
            cmd.SubscriptionId,
            subscription.CustomerId,
            cmd.TotalAmount,
            cmd.DepositAmount,
            cmd.StartDate,
            Money.DefaultCurrency);
        plan.GenerateInstallments(cmd.InstallmentCount);

        db.InstallmentPlans.Add(plan);
        await db.SaveChangesAsync(ct);

        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == plan.CustomerId)
            .Select(c => new { c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .FirstOrDefaultAsync(ct);

        var installments = plan.Installments
            .OrderBy(i => i.DueDate)
            .Select(i => new InstallmentDto(
                i.Id,
                new MoneyDto(i.Amount, plan.Currency),
                i.DueDate,
                i.PaidDate,
                i.Status.ToString()))
            .ToList();

        return new InstallmentPlanDto(
            plan.Id,
            plan.CustomerId,
            plan.CustomerSubscriptionId,
            customer is null ? "—" : $"{customer.FirstName} {customer.LastName}",
            customer?.AccountNumber ?? "—",
            new MoneyDto(plan.TotalAmount, plan.Currency),
            new MoneyDto(plan.DepositAmount, plan.Currency),
            new MoneyDto(plan.RemainingAmount, plan.Currency),
            plan.StartDate,
            installments,
            plan.CreatedAt,
            plan.UpdatedAt);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Installments;

public sealed record GetInstallmentPlanQuery(Guid Id) : IRequest<InstallmentPlanDto>;

public sealed class GetInstallmentPlanQueryHandler(IAppDbContext db) : IRequestHandler<GetInstallmentPlanQuery, InstallmentPlanDto>
{
    public async Task<InstallmentPlanDto> Handle(GetInstallmentPlanQuery query, CancellationToken ct)
    {
        var plan = await db.InstallmentPlans.AsNoTracking()
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(InstallmentPlan), query.Id);

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

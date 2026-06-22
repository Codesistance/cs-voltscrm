using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Installments;

public sealed record ListInstallmentPlansQuery(
    int Page,
    int PageSize,
    Guid? CustomerId) : IRequest<PagedResult<InstallmentPlanListItemDto>>;

public sealed class ListInstallmentPlansQueryHandler(IAppDbContext db)
    : IRequestHandler<ListInstallmentPlansQuery, PagedResult<InstallmentPlanListItemDto>>
{
    public async Task<PagedResult<InstallmentPlanListItemDto>> Handle(ListInstallmentPlansQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<InstallmentPlan> q = db.InstallmentPlans.AsNoTracking()
            .Include(p => p.Installments);
        if (query.CustomerId is { } customerId)
            q = q.Where(p => p.CustomerId == customerId);

        var total = await q.CountAsync(ct);
        var plans = await q.OrderByDescending(p => p.StartDate)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var customerIds = plans.Select(p => p.CustomerId).Distinct().ToList();
        var customers = customerIds.Count == 0
            ? new Dictionary<Guid, (string Name, string AccountNumber)>()
            : (await db.Customers.AsNoTracking()
                    .Where(c => customerIds.Contains(c.Id))
                    .Select(c => new
                    {
                        c.Id,
                        c.AccountNumber,
                        c.PersonalInfo.FirstName,
                        c.PersonalInfo.LastName,
                    })
                    .ToListAsync(ct))
                .ToDictionary(c => c.Id, c => (Name: $"{c.FirstName} {c.LastName}", AccountNumber: c.AccountNumber));

        var items = plans.Select(plan =>
            {
                customers.TryGetValue(plan.CustomerId, out var customer);
                return new InstallmentPlanListItemDto(
                    plan.Id,
                    plan.CustomerId,
                    plan.CustomerSubscriptionId,
                    customer.Name ?? "—",
                    customer.AccountNumber ?? "—",
                    new MoneyDto(plan.TotalAmount, plan.Currency),
                    new MoneyDto(plan.DepositAmount, plan.Currency),
                    new MoneyDto(plan.RemainingAmount, plan.Currency),
                    plan.StartDate,
                    plan.Installments.Count);
            })
            .ToList();

        return new PagedResult<InstallmentPlanListItemDto>(items, page, size, total);
    }
}

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Invoices;

public sealed record GenerateInvoicesCommand(int? Year, int? Month) : IRequest<IReadOnlyList<InvoiceDto>>;

public sealed class GenerateInvoicesCommandValidator : AbstractValidator<GenerateInvoicesCommand>
{
    public GenerateInvoicesCommandValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 3000).When(x => x.Year.HasValue);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).When(x => x.Month.HasValue);
    }
}

public sealed class GenerateInvoicesCommandHandler(IAppDbContext db)
    : IRequestHandler<GenerateInvoicesCommand, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(GenerateInvoicesCommand cmd, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var year = cmd.Year ?? now.Year;
        var month = cmd.Month ?? now.Month;
        var periodStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
        var dueDate = periodStart.AddMonths(1).AddDays(-1);

        var subscriptions = await db.CustomerSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                        && s.StartDate <= periodEnd
                        && (s.EndDate == null || s.EndDate >= periodStart))
            .ToListAsync(ct);
        if (subscriptions.Count == 0)
            return [];

        var planIds = subscriptions.Select(s => s.ServicePlanId).Distinct().ToList();
        var plans = await db.ServicePlans.AsNoTracking()
            .Where(p => planIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, Amount = p.BasePrice.Amount, Currency = p.BasePrice.Currency })
            .ToListAsync(ct);
        var planLookup = plans.ToDictionary(p => p.Id);

        var subscriptionIds = subscriptions.Select(s => s.Id).ToList();
        var alreadyInvoiced = await db.Invoices.AsNoTracking()
            .Where(i => i.PeriodYear == year
                        && i.PeriodMonth == month
                        && subscriptionIds.Contains(i.CustomerSubscriptionId))
            .Select(i => i.CustomerSubscriptionId)
            .ToListAsync(ct);
        var alreadySet = alreadyInvoiced.ToHashSet();

        var customerIds = subscriptions.Select(s => s.CustomerId).Distinct().ToList();
        var grants = await db.DiscountGrants
            .Where(g => customerIds.Contains(g.CustomerId)
                        && g.Status == DiscountGrantStatus.Active
                        && g.IsRecurring
                        && g.ValidFrom <= periodEnd
                        && (g.ValidUntil == null || g.ValidUntil >= periodStart))
            .ToListAsync(ct);

        var created = new List<Invoice>();
        foreach (var subscription in subscriptions)
        {
            if (alreadySet.Contains(subscription.Id))
                continue;
            if (!planLookup.TryGetValue(subscription.ServicePlanId, out var plan))
                continue;

            var grossMoney = subscription.NegotiatedPrice ?? new Money(plan.Amount, plan.Currency);
            var gross = grossMoney.Amount;

            var invoice = new Invoice(
                subscription.Id,
                subscription.CustomerId,
                year,
                month,
                gross,
                dueDate,
                grossMoney.Currency);

            invoice.AddLineItem($"{plan.Name} charge ({year}-{month:D2})", gross);

            var applicable = grants.Where(g =>
                    g.CustomerId == subscription.CustomerId
                    && (g.Scope == DiscountScope.Invoice || g.Scope == DiscountScope.Subscription)
                    && (g.TargetId == null || g.TargetId == subscription.Id || g.TargetId == subscription.ServicePlanId))
                .ToList();

            decimal totalDiscount = 0m;
            foreach (var grant in applicable)
            {
                var discount = Math.Min(gross - totalDiscount, grant.CalculateDiscountAmount(gross));
                if (discount <= 0m)
                    continue;

                totalDiscount += discount;
                invoice.AddLineItem($"Discount ({grant.DiscountType})", -discount, isDiscount: true);
            }

            created.Add(invoice);
            db.Invoices.Add(invoice);
        }

        if (created.Count == 0)
            return [];

        await db.SaveChangesAsync(ct);

        // B8 fix: Batch-load customers to avoid N+1 query in ToDetailDtoAsync
        var createdCustomerIds = created.Select(i => i.CustomerId).Distinct().ToList();
        var customers = await db.Customers.AsNoTracking()
            .Where(c => createdCustomerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .ToDictionaryAsync(c => c.Id, ct);

        var results = new List<InvoiceDto>(created.Count);
        foreach (var invoice in created)
        {
            customers.TryGetValue(invoice.CustomerId, out var cust);
            results.Add(invoice.ToDetailDto(
                cust is null ? "—" : $"{cust.FirstName} {cust.LastName}",
                cust?.AccountNumber ?? "—"));
        }

        return results;
    }
}

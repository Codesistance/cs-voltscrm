using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalSummaryQuery(Guid CustomerId) : IRequest<PortalSummaryDto>;

public sealed class PortalSummaryQueryHandler(IAppDbContext db) : IRequestHandler<PortalSummaryQuery, PortalSummaryDto>
{
    public async Task<PortalSummaryDto> Handle(PortalSummaryQuery query, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var mtdStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == query.CustomerId)
            .Select(i => new { Balance = i.GrossAmount - i.DiscountAmount - i.AmountPaid, i.Currency, i.Status })
            .ToListAsync(ct);

        var paidThisMonth = await db.Payments.AsNoTracking()
            .Where(p => p.CustomerId == query.CustomerId
                        && p.Status == PaymentStatus.Completed
                        && p.PaymentDate >= mtdStart)
            .Select(p => new { p.NetAmount, p.Currency })
            .ToListAsync(ct);

        var activeSubscriptions = await db.CustomerSubscriptions.AsNoTracking()
            .CountAsync(s => s.CustomerId == query.CustomerId && s.Status == SubscriptionStatus.Active, ct);

        var currency = invoices.FirstOrDefault()?.Currency
                       ?? paidThisMonth.FirstOrDefault()?.Currency
                       ?? Money.DefaultCurrency;

        return new PortalSummaryDto(
            new MoneyDto(invoices.Sum(i => i.Balance), currency),
            invoices.Count(i => i.Status is InvoiceStatus.Pending or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue),
            activeSubscriptions,
            new MoneyDto(paidThisMonth.Sum(p => p.NetAmount), currency));
    }
}

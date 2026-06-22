using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Reports;

public sealed record DashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public sealed class DashboardSummaryQueryHandler(IAppDbContext db) : IRequestHandler<DashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(DashboardSummaryQuery query, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var mtdStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var activeCustomers = await db.Customers.AsNoTracking()
            .CountAsync(c => c.Status == CustomerStatus.Active, ct);

        // Materialize invoices with balance > 0 then sum in-memory
        var invoicesWithBalance = await db.Invoices.AsNoTracking()
            .Where(i => i.GrossAmount - i.DiscountAmount - i.AmountPaid > 0m)
            .Select(i => new { Balance = i.GrossAmount - i.DiscountAmount - i.AmountPaid, i.Status, i.DueDate })
            .ToListAsync(ct);

        var outstanding = invoicesWithBalance.Sum(i => i.Balance);
        var overdue = invoicesWithBalance.Count(i => i.Status == InvoiceStatus.Overdue || i.DueDate < now);

        // Materialize payments then sum in-memory
        var completedPayments = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed && p.PaymentDate >= mtdStart)
            .Select(p => p.NetAmount)
            .ToListAsync(ct);

        var collectionsMtd = completedPayments.Sum();

        var currency = Money.DefaultCurrency;
        return new DashboardSummaryDto(
            activeCustomers,
            new MoneyDto(outstanding, currency),
            new MoneyDto(collectionsMtd, currency),
            overdue);
    }
}

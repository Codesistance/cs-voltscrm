using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;

namespace VoltsCRM.Application.Features.Reports;

public sealed record AgingReportQuery : IRequest<AgingReportDto>;

public sealed class AgingReportQueryHandler(IAppDbContext db) : IRequestHandler<AgingReportQuery, AgingReportDto>
{
    private sealed record AgingInvoice(DateTimeOffset DueDate, decimal Balance, string Currency);

    public async Task<AgingReportDto> Handle(AgingReportQuery query, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.GrossAmount - i.DiscountAmount - i.AmountPaid > 0m)
            .Select(i => new AgingInvoice(i.DueDate, i.GrossAmount - i.DiscountAmount - i.AmountPaid, i.Currency))
            .ToListAsync(ct);

        var currency = invoices.FirstOrDefault()?.Currency ?? Money.DefaultCurrency;
        var buckets = new List<AgingBucketDto>
        {
            BuildBucket("0-30", invoices, now, 0, 30, currency),
            BuildBucket("31-60", invoices, now, 31, 60, currency),
            BuildBucket("61-90", invoices, now, 61, 90, currency),
            BuildBucket("90+", invoices, now, 91, int.MaxValue, currency),
        };

        return new AgingReportDto(buckets);
    }

    private static AgingBucketDto BuildBucket(
        string label,
        IReadOnlyList<AgingInvoice> invoices,
        DateTimeOffset now,
        int minDays,
        int maxDays,
        string currency)
    {
        var filtered = invoices
            .Where(i =>
            {
                var days = (int)Math.Floor((now - i.DueDate).TotalDays);
                return days >= minDays && days <= maxDays;
            })
            .ToList();

        return new AgingBucketDto(
            label,
            filtered.Count,
            new MoneyDto(filtered.Sum(i => i.Balance), currency));
    }
}

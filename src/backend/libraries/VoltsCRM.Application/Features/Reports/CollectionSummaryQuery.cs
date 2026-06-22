using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Reports;

public sealed record CollectionSummaryQuery(DateOnly From, DateOnly To) : IRequest<CollectionSummaryDto>;

public sealed class CollectionSummaryQueryValidator : AbstractValidator<CollectionSummaryQuery>
{
    public CollectionSummaryQueryValidator()
    {
        RuleFor(x => x).Must(x => x.To >= x.From)
            .WithMessage("To date must be on or after From date.");
    }
}

public sealed class CollectionSummaryQueryHandler(IAppDbContext db)
    : IRequestHandler<CollectionSummaryQuery, CollectionSummaryDto>
{
    public async Task<CollectionSummaryDto> Handle(CollectionSummaryQuery query, CancellationToken ct)
    {
        var from = query.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toExclusive = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rows = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed
                        && p.PaymentDate >= from
                        && p.PaymentDate < toExclusive)
            .Select(p => new { Day = DateOnly.FromDateTime(p.PaymentDate.UtcDateTime), p.NetAmount, p.Currency })
            .ToListAsync(ct);

        var currency = rows.FirstOrDefault()?.Currency ?? Money.DefaultCurrency;
        var daily = rows
            .GroupBy(r => r.Day)
            .OrderBy(g => g.Key)
            .Select(g => new CollectionSummaryItemDto(
                g.Key,
                g.Count(),
                new MoneyDto(g.Sum(x => x.NetAmount), currency)))
            .ToList();

        return new CollectionSummaryDto(
            query.From,
            query.To,
            new MoneyDto(rows.Sum(r => r.NetAmount), currency),
            daily);
    }
}

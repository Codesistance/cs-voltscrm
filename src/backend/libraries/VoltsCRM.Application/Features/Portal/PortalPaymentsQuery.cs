using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalPaymentsQuery(Guid CustomerId, int Page, int PageSize) : IRequest<PagedResult<PortalPaymentDto>>;

public sealed class PortalPaymentsQueryHandler(IAppDbContext db)
    : IRequestHandler<PortalPaymentsQuery, PagedResult<PortalPaymentDto>>
{
    public async Task<PagedResult<PortalPaymentDto>> Handle(PortalPaymentsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var baseQuery = db.Payments.AsNoTracking()
            .Where(p => p.CustomerId == query.CustomerId);

        var total = await baseQuery.CountAsync(ct);
        var payments = await baseQuery
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = payments.Select(p => new PortalPaymentDto(
                p.Id,
                new MoneyDto(p.Amount, p.Currency),
                new MoneyDto(p.NetAmount, p.Currency),
                p.Method.ToString(),
                p.Channel.ToString(),
                p.Status.ToString(),
                p.PaymentDate,
                p.PlatformReference))
            .ToList();

        return new PagedResult<PortalPaymentDto>(items, page, size, total);
    }
}

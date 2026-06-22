using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalInvoicesQuery(Guid CustomerId, int Page, int PageSize) : IRequest<PagedResult<PortalInvoiceDto>>;

public sealed class PortalInvoicesQueryHandler(IAppDbContext db)
    : IRequestHandler<PortalInvoicesQuery, PagedResult<PortalInvoiceDto>>
{
    public async Task<PagedResult<PortalInvoiceDto>> Handle(PortalInvoicesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var baseQuery = db.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == query.CustomerId);

        var total = await baseQuery.CountAsync(ct);
        var invoices = await baseQuery
            .OrderByDescending(i => i.PeriodYear)
            .ThenByDescending(i => i.PeriodMonth)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = invoices.Select(i => new PortalInvoiceDto(
                i.Id,
                i.PeriodYear,
                i.PeriodMonth,
                new MoneyDto(i.AmountDue, i.Currency),
                new MoneyDto(i.AmountPaid, i.Currency),
                new MoneyDto(i.Balance, i.Currency),
                i.DueDate,
                i.Status.ToString()))
            .ToList();

        return new PagedResult<PortalInvoiceDto>(items, page, size, total);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Invoices;

public sealed record ListInvoicesQuery(
    int Page,
    int PageSize,
    Guid? CustomerId,
    string? Status,
    int? Year,
    int? Month) : IRequest<PagedResult<InvoiceListItemDto>>;

public sealed class ListInvoicesQueryHandler(IAppDbContext db)
    : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceListItemDto>>
{
    public async Task<PagedResult<InvoiceListItemDto>> Handle(ListInvoicesQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Invoice> q = db.Invoices.AsNoTracking();

        if (query.CustomerId is { } customerId)
            q = q.Where(i => i.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<InvoiceStatus>(query.Status, true, out var status))
            q = q.Where(i => i.Status == status);
        if (query.Year is { } year)
            q = q.Where(i => i.PeriodYear == year);
        if (query.Month is { } month)
            q = q.Where(i => i.PeriodMonth == month);

        var total = await q.CountAsync(ct);
        var invoices = await q.OrderByDescending(i => i.PeriodYear)
            .ThenByDescending(i => i.PeriodMonth)
            .ThenByDescending(i => i.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
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
                .ToDictionary(
                    c => c.Id,
                    c => (Name: $"{c.FirstName} {c.LastName}", AccountNumber: c.AccountNumber));

        var items = invoices.Select(invoice =>
            {
                customers.TryGetValue(invoice.CustomerId, out var customer);
                return invoice.ToListItem(customer.Name ?? "—", customer.AccountNumber ?? "—");
            })
            .ToList();

        return new PagedResult<InvoiceListItemDto>(items, page, size, total);
    }
}

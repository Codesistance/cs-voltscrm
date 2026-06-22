using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Customers;

public sealed record ListCustomersQuery(int Page, int PageSize, string? Q, string? Status)
    : IRequest<PagedResult<CustomerListItemDto>>;

public sealed class ListCustomersQueryHandler(IAppDbContext db)
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListItemDto>>
{
    public async Task<PagedResult<CustomerListItemDto>> Handle(ListCustomersQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Customer> q = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<CustomerStatus>(query.Status, true, out var status))
            q = q.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            q = q.Where(c =>
                c.AccountNumber.ToLower().Contains(term) ||
                c.PersonalInfo.FirstName.ToLower().Contains(term) ||
                c.PersonalInfo.LastName.ToLower().Contains(term) ||
                c.PersonalInfo.Phone.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var customers = await q.OrderBy(c => c.AccountNumber)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return new PagedResult<CustomerListItemDto>(
            customers.Select(c => c.ToListItem()).ToList(),
            page, size, total);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Discounts;

public sealed record ListDiscountGrantsQuery(
    int Page,
    int PageSize,
    Guid? CustomerId,
    string? Status) : IRequest<PagedResult<DiscountGrantDto>>;

public sealed class ListDiscountGrantsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListDiscountGrantsQuery, PagedResult<DiscountGrantDto>>
{
    public async Task<PagedResult<DiscountGrantDto>> Handle(ListDiscountGrantsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<DiscountGrant> q = db.DiscountGrants.AsNoTracking();

        if (query.CustomerId is { } customerId)
            q = q.Where(g => g.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<DiscountGrantStatus>(query.Status, true, out var status))
            q = q.Where(g => g.Status == status);

        var total = await q.CountAsync(ct);

        // B8 fix: Join with customers to get CustomerName
        var grantsWithCustomers = await q
            .OrderByDescending(g => g.GrantedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Join(db.Customers,
                g => g.CustomerId,
                c => c.Id,
                (g, c) => new { Grant = g, CustomerName = c.PersonalInfo.FirstName + " " + c.PersonalInfo.LastName })
            .ToListAsync(ct);

        var items = grantsWithCustomers.Select(x => new DiscountGrantDto(
                x.Grant.Id,
                x.Grant.CustomerId,
                x.CustomerName,
                x.Grant.DiscountType.ToString(),
                x.Grant.Value,
                x.Grant.Scope.ToString(),
                x.Grant.TargetId,
                x.Grant.IsRecurring,
                x.Grant.ValidFrom,
                x.Grant.ValidUntil,
                x.Grant.GrantedByUserId,
                x.Grant.GrantedAt,
                x.Grant.Reason,
                x.Grant.Status.ToString(),
                x.Grant.CreatedAt,
                x.Grant.UpdatedAt))
            .ToList();

        return new PagedResult<DiscountGrantDto>(items, page, size, total);
    }
}

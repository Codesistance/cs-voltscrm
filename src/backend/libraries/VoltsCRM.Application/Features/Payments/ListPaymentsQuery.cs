using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Payments;

public sealed record ListPaymentsQuery(
    int Page, int PageSize, Guid? CustomerId, string? Status, string? Method,
    DateTimeOffset? From, DateTimeOffset? To) : IRequest<PagedResult<PaymentListItemDto>>;

public sealed class ListPaymentsQueryHandler(IAppDbContext db)
    : IRequestHandler<ListPaymentsQuery, PagedResult<PaymentListItemDto>>
{
    public async Task<PagedResult<PaymentListItemDto>> Handle(ListPaymentsQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        IQueryable<Payment> q = db.Payments.AsNoTracking();

        if (query.CustomerId is { } customerId)
            q = q.Where(p => p.CustomerId == customerId);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<PaymentStatus>(query.Status, true, out var status))
            q = q.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Method) && Enum.TryParse<PaymentMethod>(query.Method, true, out var method))
            q = q.Where(p => p.Method == method);
        if (query.From is { } from)
            q = q.Where(p => p.PaymentDate >= from);
        if (query.To is { } to)
            q = q.Where(p => p.PaymentDate <= to);

        var total = await q.CountAsync(ct);
        var payments = await q.OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        var customerIds = payments.Select(p => p.CustomerId).Distinct().ToList();
        var names = (await db.Customers.AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .Select(c => new { c.Id, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
                .ToListAsync(ct))
            .ToDictionary(c => c.Id, c => $"{c.FirstName} {c.LastName}");

        var items = payments.Select(p => p.ToListItem(names.GetValueOrDefault(p.CustomerId, "—"))).ToList();
        return new PagedResult<PaymentListItemDto>(items, page, size, total);
    }
}

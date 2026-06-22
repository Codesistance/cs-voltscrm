using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record ListServicePlansQuery(int Page, int PageSize, string? Q, string? Status)
    : IRequest<PagedResult<ServicePlanListItemDto>>;

public sealed class ListServicePlansQueryHandler(IAppDbContext db)
    : IRequestHandler<ListServicePlansQuery, PagedResult<ServicePlanListItemDto>>
{
    public async Task<PagedResult<ServicePlanListItemDto>> Handle(ListServicePlansQuery query, CancellationToken ct)
    {
        var page = Math.Max(1, query.Page);
        var size = Math.Clamp(query.PageSize, 1, 100);

        var q = db.ServicePlans.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<PlanStatus>(query.Status, true, out var status))
        {
            q = q.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim().ToLower();
            q = q.Where(p => p.PlanCode.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        var plans = await q.Include(p => p.LineItems)
            .OrderBy(p => p.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return new PagedResult<ServicePlanListItemDto>(
            plans.Select(p => p.ToListItem()).ToList(), page, size, total);
    }
}

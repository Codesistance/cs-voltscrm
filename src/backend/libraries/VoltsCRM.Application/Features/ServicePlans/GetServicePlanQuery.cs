using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record GetServicePlanQuery(Guid Id) : IRequest<ServicePlanDto>;

public sealed class GetServicePlanQueryHandler(IAppDbContext db)
    : IRequestHandler<GetServicePlanQuery, ServicePlanDto>
{
    public async Task<ServicePlanDto> Handle(GetServicePlanQuery query, CancellationToken ct)
    {
        var plan = await db.ServicePlans.AsNoTracking()
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(ServicePlan), query.Id);

        return await plan.ToDetailDtoAsync(db, ct);
    }
}

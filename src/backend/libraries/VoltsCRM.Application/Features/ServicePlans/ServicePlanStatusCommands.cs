using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.ServicePlans;

public sealed record ArchiveServicePlanCommand(Guid Id) : IRequest;

public sealed record RestoreServicePlanCommand(Guid Id) : IRequest;

public sealed class ArchiveServicePlanHandler(IAppDbContext db) : IRequestHandler<ArchiveServicePlanCommand>
{
    public async Task Handle(ArchiveServicePlanCommand cmd, CancellationToken ct)
    {
        var plan = await db.ServicePlans.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(ServicePlan), cmd.Id);
        plan.Archive();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class RestoreServicePlanHandler(IAppDbContext db) : IRequestHandler<RestoreServicePlanCommand>
{
    public async Task Handle(RestoreServicePlanCommand cmd, CancellationToken ct)
    {
        var plan = await db.ServicePlans.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(ServicePlan), cmd.Id);
        plan.Restore();
        await db.SaveChangesAsync(ct);
    }
}

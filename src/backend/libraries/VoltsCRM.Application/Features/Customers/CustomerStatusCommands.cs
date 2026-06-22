using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Application.Features.Customers;

public sealed record SuspendCustomerCommand(Guid Id) : IRequest;

public sealed record ReactivateCustomerCommand(Guid Id) : IRequest;

public sealed record DisconnectCustomerCommand(Guid Id) : IRequest;

public sealed class SuspendCustomerHandler(IAppDbContext db) : IRequestHandler<SuspendCustomerCommand>
{
    public async Task Handle(SuspendCustomerCommand cmd, CancellationToken ct)
    {
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), cmd.Id);
        c.Suspend();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ReactivateCustomerHandler(IAppDbContext db) : IRequestHandler<ReactivateCustomerCommand>
{
    public async Task Handle(ReactivateCustomerCommand cmd, CancellationToken ct)
    {
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), cmd.Id);
        c.Reactivate();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class DisconnectCustomerHandler(IAppDbContext db) : IRequestHandler<DisconnectCustomerCommand>
{
    public async Task Handle(DisconnectCustomerCommand cmd, CancellationToken ct)
    {
        var c = await db.Customers.FirstOrDefaultAsync(x => x.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), cmd.Id);
        c.Disconnect();
        await db.SaveChangesAsync(ct);
    }
}

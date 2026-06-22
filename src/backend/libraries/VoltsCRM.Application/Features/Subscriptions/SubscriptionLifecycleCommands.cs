using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record ActivateSubscriptionCommand(Guid Id) : IRequest;
public sealed record SuspendSubscriptionCommand(Guid Id) : IRequest;
public sealed record ResumeSubscriptionCommand(Guid Id) : IRequest;
public sealed record TerminateSubscriptionCommand(Guid Id) : IRequest;

public sealed class ActivateSubscriptionHandler(IAppDbContext db) : IRequestHandler<ActivateSubscriptionCommand>
{
    public Task Handle(ActivateSubscriptionCommand cmd, CancellationToken ct)
        => SubscriptionLifecycle.RunAsync(db, cmd.Id, s => s.Activate(), ct);
}

public sealed class SuspendSubscriptionHandler(IAppDbContext db) : IRequestHandler<SuspendSubscriptionCommand>
{
    public Task Handle(SuspendSubscriptionCommand cmd, CancellationToken ct)
        => SubscriptionLifecycle.RunAsync(db, cmd.Id, s => s.Suspend(), ct);
}

public sealed class ResumeSubscriptionHandler(IAppDbContext db) : IRequestHandler<ResumeSubscriptionCommand>
{
    public Task Handle(ResumeSubscriptionCommand cmd, CancellationToken ct)
        => SubscriptionLifecycle.RunAsync(db, cmd.Id, s => s.Resume(), ct);
}

public sealed class TerminateSubscriptionHandler(IAppDbContext db) : IRequestHandler<TerminateSubscriptionCommand>
{
    public Task Handle(TerminateSubscriptionCommand cmd, CancellationToken ct)
        => SubscriptionLifecycle.RunAsync(db, cmd.Id, s => s.Terminate(DateTimeOffset.UtcNow), ct);
}

internal static class SubscriptionLifecycle
{
    /// <summary>Loads the subscription, applies a state transition, and translates the domain's
    /// <see cref="InvalidOperationException"/> into a clean 400 (reusing the domain message).</summary>
    public static async Task RunAsync(
        IAppDbContext db, Guid id, Action<CustomerSubscription> transition, CancellationToken ct)
    {
        var sub = await db.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException(nameof(CustomerSubscription), id);

        try
        {
            transition(sub);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(new[] { new ValidationFailure("status", ex.Message) });
        }

        await db.SaveChangesAsync(ct);
    }
}

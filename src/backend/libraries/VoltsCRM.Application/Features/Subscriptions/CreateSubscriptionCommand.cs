using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    Guid ServicePlanId,
    DateTimeOffset StartDate,
    MoneyDto? NegotiatedPrice,
    Guid? ServiceLocationId) : IRequest<SubscriptionDto>;

public sealed class CreateSubscriptionValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ServicePlanId).NotEmpty();
        RuleFor(x => x.NegotiatedPrice!.Amount).GreaterThanOrEqualTo(0).When(x => x.NegotiatedPrice is not null);
        RuleFor(x => x.NegotiatedPrice!.Currency).NotEmpty().Length(3).When(x => x.NegotiatedPrice is not null);
    }
}

public sealed class CreateSubscriptionHandler(IAppDbContext db, IOptions<BillingOptions> billing)
    : IRequestHandler<CreateSubscriptionCommand, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(CreateSubscriptionCommand cmd, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == cmd.CustomerId, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateSubscriptionCommand.CustomerId), "Customer does not exist."),
            });

        var plan = await db.ServicePlans.AsNoTracking()
            .Where(p => p.Id == cmd.ServicePlanId)
            .Select(p => new { p.BillingType, p.Status })
            .FirstOrDefaultAsync(ct);
        if (plan is null)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateSubscriptionCommand.ServicePlanId), "Service plan does not exist."),
            });
        if (plan.Status != PlanStatus.Active)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateSubscriptionCommand.ServicePlanId), "Service plan is archived."),
            });

        var negotiated = cmd.NegotiatedPrice is null
            ? null
            : new Money(cmd.NegotiatedPrice.Amount, cmd.NegotiatedPrice.Currency);

        // BillingType is copied from the plan at subscription time.
        var sub = new CustomerSubscription(cmd.CustomerId, cmd.ServicePlanId, plan.BillingType, cmd.StartDate, negotiated);
        db.CustomerSubscriptions.Add(sub);

        if (cmd.ServiceLocationId is { } locationId)
        {
            var location = await db.ServiceLocations
                .FirstOrDefaultAsync(l => l.Id == locationId && l.CustomerId == cmd.CustomerId, ct)
                ?? throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(CreateSubscriptionCommand.ServiceLocationId),
                        "Service location does not belong to this customer."),
                });
            location.LinkSubscription(sub.Id);
        }

        await db.SaveChangesAsync(ct);
        return await sub.ToDetailDtoAsync(db, billing.Value.DefaultCurrency, ct);
    }
}

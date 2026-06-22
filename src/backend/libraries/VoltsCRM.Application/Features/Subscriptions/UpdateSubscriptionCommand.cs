using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record UpdateSubscriptionCommand(
    Guid Id,
    DateTimeOffset StartDate,
    MoneyDto? NegotiatedPrice,
    Guid? ServiceLocationId) : IRequest<SubscriptionDto>;

public sealed class UpdateSubscriptionValidator : AbstractValidator<UpdateSubscriptionCommand>
{
    public UpdateSubscriptionValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NegotiatedPrice!.Amount).GreaterThanOrEqualTo(0).When(x => x.NegotiatedPrice is not null);
        RuleFor(x => x.NegotiatedPrice!.Currency).NotEmpty().Length(3).When(x => x.NegotiatedPrice is not null);
    }
}

public sealed class UpdateSubscriptionHandler(IAppDbContext db, IOptions<BillingOptions> billing)
    : IRequestHandler<UpdateSubscriptionCommand, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(UpdateSubscriptionCommand cmd, CancellationToken ct)
    {
        var sub = await db.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == cmd.Id, ct)
            ?? throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateSubscriptionCommand.Id), "Subscription does not exist."),
            });

        if (sub.Status != SubscriptionStatus.Pending)
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateSubscriptionCommand.Id), "Only pending subscriptions can be edited."),
            });

        var negotiated = cmd.NegotiatedPrice is null
            ? null
            : new Money(cmd.NegotiatedPrice.Amount, cmd.NegotiatedPrice.Currency);

        sub.UpdatePendingDetails(cmd.StartDate, negotiated);

        if (cmd.ServiceLocationId is { } locationId)
        {
            var location = await db.ServiceLocations
                .FirstOrDefaultAsync(l => l.Id == locationId && l.CustomerId == sub.CustomerId, ct)
                ?? throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(UpdateSubscriptionCommand.ServiceLocationId),
                        "Service location does not belong to this customer."),
                });
            location.LinkSubscription(sub.Id);
        }

        await db.SaveChangesAsync(ct);
        return await sub.ToDetailDtoAsync(db, billing.Value.DefaultCurrency, ct);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Subscriptions;

public sealed record GetSubscriptionQuery(Guid Id) : IRequest<SubscriptionDto>;

public sealed class GetSubscriptionQueryHandler(IAppDbContext db, IOptions<BillingOptions> billing)
    : IRequestHandler<GetSubscriptionQuery, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(GetSubscriptionQuery query, CancellationToken ct)
    {
        var sub = await db.CustomerSubscriptions.AsNoTracking()
            .Include(s => s.DeployedItems)
            .FirstOrDefaultAsync(s => s.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(CustomerSubscription), query.Id);

        return await sub.ToDetailDtoAsync(db, billing.Value.DefaultCurrency, ct);
    }
}

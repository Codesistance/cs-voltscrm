using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Discounts;

public sealed record RevokeDiscountCommand(Guid Id) : IRequest;

public sealed class RevokeDiscountCommandHandler(IAppDbContext db) : IRequestHandler<RevokeDiscountCommand>
{
    public async Task Handle(RevokeDiscountCommand command, CancellationToken ct)
    {
        var grant = await db.DiscountGrants.FirstOrDefaultAsync(g => g.Id == command.Id, ct)
            ?? throw new NotFoundException(nameof(DiscountGrant), command.Id);

        grant.Revoke();
        await db.SaveChangesAsync(ct);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Payments;

public sealed record GetPaymentQuery(Guid Id) : IRequest<PaymentDto>;

public sealed class GetPaymentQueryHandler(IAppDbContext db) : IRequestHandler<GetPaymentQuery, PaymentDto>
{
    public async Task<PaymentDto> Handle(GetPaymentQuery query, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking()
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(Payment), query.Id);

        return await payment.ToDetailDtoAsync(db, ct);
    }
}

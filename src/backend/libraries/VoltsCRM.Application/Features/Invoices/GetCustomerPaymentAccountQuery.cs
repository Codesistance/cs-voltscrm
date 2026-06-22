using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Application.Features.Invoices;

public sealed record GetCustomerPaymentAccountQuery(Guid CustomerId) : IRequest<PaymentAccountDto>;

public sealed class GetCustomerPaymentAccountQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerPaymentAccountQuery, PaymentAccountDto>
{
    public async Task<PaymentAccountDto> Handle(GetCustomerPaymentAccountQuery query, CancellationToken ct)
    {
        var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == query.CustomerId, ct);
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), query.CustomerId);

        var account = await db.PaymentAccounts.FirstOrDefaultAsync(a => a.CustomerId == query.CustomerId, ct);
        if (account is null)
        {
            account = new PaymentAccount(query.CustomerId);
            db.PaymentAccounts.Add(account);
            await db.SaveChangesAsync(ct);
        }

        return account.ToDto();
    }
}

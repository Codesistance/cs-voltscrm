using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Application.Features.Customers;

public sealed record GetCustomerQuery(Guid Id) : IRequest<CustomerDto>;

public sealed class GetCustomerQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Include(c => c.ServiceLocations)
            .FirstOrDefaultAsync(c => c.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), query.Id);

        return customer.ToDto();
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Invoices;

public sealed record GetInvoiceQuery(Guid Id) : IRequest<InvoiceDto>;

public sealed class GetInvoiceQueryHandler(IAppDbContext db) : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == query.Id, ct)
            ?? throw new NotFoundException(nameof(Invoice), query.Id);

        return await invoice.ToDetailDtoAsync(db, ct);
    }
}

using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Crm;

namespace VoltsCRM.Application.Features.Portal;

public sealed record PortalProfileQuery(Guid CustomerId) : IRequest<PortalProfileDto>;

public sealed class PortalProfileQueryHandler(IAppDbContext db)
    : IRequestHandler<PortalProfileQuery, PortalProfileDto>
{
    public async Task<PortalProfileDto> Handle(PortalProfileQuery query, CancellationToken ct)
    {
        // Project owned scalar columns (FullName is a computed property → not SQL-translatable).
        var c = await db.Customers.AsNoTracking()
            .Where(x => x.Id == query.CustomerId)
            .Select(x => new
            {
                x.AccountNumber,
                x.PersonalInfo.FirstName,
                x.PersonalInfo.LastName,
                x.PersonalInfo.Phone,
                x.PersonalInfo.Email,
                x.Status,
                x.Location.Address.Street,
                x.Location.Address.City,
                x.Location.Address.Region,
                x.Location.Address.Country,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(nameof(Customer), query.CustomerId);

        return new PortalProfileDto(
            c.AccountNumber,
            $"{c.FirstName} {c.LastName}".Trim(),
            c.Phone,
            c.Email,
            c.Status.ToString(),
            new AddressDto(c.Street, c.City, c.Region, c.Country));
    }
}

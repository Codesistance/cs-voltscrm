using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Crm;
using static VoltsCRM.Application.Common.Mappings.LocationMappings;
using static VoltsCRM.Application.Features.Customers.CustomerMapping;

namespace VoltsCRM.Application.Features.Customers;

// AccountNumber is immutable after creation (no domain setter), so it's not updatable here.
public sealed record UpdateCustomerCommand(
    Guid Id,
    PersonalInfoInput PersonalInfo,
    LocationInput Location) : IRequest<CustomerDto>;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.PersonalInfo).NotNull().SetValidator(new PersonalInfoValidator());
        RuleFor(x => x.Location).NotNull().SetValidator(new LocationValidator());
    }
}

public sealed class UpdateCustomerHandler(IAppDbContext db) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await db.Customers.Include(c => c.ServiceLocations)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Customer), cmd.Id);

        customer.UpdatePersonalInfo(ToPersonalInfo(cmd.PersonalInfo));
        customer.UpdateLocation(ToLocation(cmd.Location));

        await db.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}

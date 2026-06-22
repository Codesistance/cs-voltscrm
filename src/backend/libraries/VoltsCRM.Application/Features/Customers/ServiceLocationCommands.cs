using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Crm;
using static VoltsCRM.Application.Common.Mappings.LocationMappings;

namespace VoltsCRM.Application.Features.Customers;

public sealed record AddServiceLocationCommand(
    Guid CustomerId, string Label, LocationInput Location) : IRequest<ServiceLocationDto>;

public sealed class AddServiceLocationValidator : AbstractValidator<AddServiceLocationCommand>
{
    public AddServiceLocationValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Location).NotNull().SetValidator(new LocationValidator());
    }
}

public sealed class AddServiceLocationHandler(IAppDbContext db)
    : IRequestHandler<AddServiceLocationCommand, ServiceLocationDto>
{
    public async Task<ServiceLocationDto> Handle(AddServiceLocationCommand cmd, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == cmd.CustomerId, ct)
            ?? throw new NotFoundException(nameof(Customer), cmd.CustomerId);

        var location = customer.AddServiceLocation(cmd.Label, ToLocation(cmd.Location));
        db.ServiceLocations.Add(location); // explicit Add → INSERT (key is app-generated, not store-generated)
        await db.SaveChangesAsync(ct);
        return location.ToDto();
    }
}

public sealed record DeactivateServiceLocationCommand(Guid CustomerId, Guid LocationId) : IRequest;

public sealed class DeactivateServiceLocationHandler(IAppDbContext db)
    : IRequestHandler<DeactivateServiceLocationCommand>
{
    public async Task Handle(DeactivateServiceLocationCommand cmd, CancellationToken ct)
    {
        var location = await db.ServiceLocations
            .FirstOrDefaultAsync(s => s.Id == cmd.LocationId && s.CustomerId == cmd.CustomerId, ct)
            ?? throw new NotFoundException(nameof(ServiceLocation), cmd.LocationId);

        location.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}

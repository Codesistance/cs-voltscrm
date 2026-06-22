using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Crm;
using static VoltsCRM.Application.Common.Mappings.LocationMappings;
using static VoltsCRM.Application.Features.Customers.CustomerMapping;

namespace VoltsCRM.Application.Features.Customers;

public sealed record CreateCustomerCommand(
    string AccountNumber,
    PersonalInfoInput PersonalInfo,
    LocationInput Location) : IRequest<CustomerDto>;

public sealed class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.PersonalInfo).NotNull().SetValidator(new PersonalInfoValidator());
        RuleFor(x => x.Location).NotNull().SetValidator(new LocationValidator());
    }
}

public sealed class CreateCustomerHandler(IAppDbContext db) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        if (await db.Customers.AnyAsync(c => c.AccountNumber == cmd.AccountNumber, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateCustomerCommand.AccountNumber),
                    $"Account number '{cmd.AccountNumber}' is already in use."),
            });

        var customer = new Customer(cmd.AccountNumber, ToPersonalInfo(cmd.PersonalInfo), ToLocation(cmd.Location));

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}

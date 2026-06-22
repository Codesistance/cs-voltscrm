using FluentValidation;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Customers;

/// <summary>Reused (via SetValidator) by the customer + service-location commands.</summary>
internal sealed class PersonalInfoValidator : AbstractValidator<PersonalInfoInput>
{
    public PersonalInfoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.Gender).Must(g => Enum.TryParse<Gender>(g, true, out _)).WithMessage("Invalid gender.");
    }
}

internal sealed class AddressValidator : AbstractValidator<AddressInput>
{
    public AddressValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).MaximumLength(200);
        RuleFor(x => x.Region).MaximumLength(100);
    }
}

/// <summary>Reused (via SetValidator) by the customer + agent commands.</summary>
internal sealed class LocationValidator : AbstractValidator<LocationInput>
{
    public LocationValidator()
    {
        RuleFor(x => x.Address).NotNull().SetValidator(new AddressValidator());
        When(x => x.Coordinates is not null, () =>
        {
            RuleFor(x => x.Coordinates!.Latitude).InclusiveBetween(-90d, 90d);
            RuleFor(x => x.Coordinates!.Longitude).InclusiveBetween(-180d, 180d);
        });
    }
}

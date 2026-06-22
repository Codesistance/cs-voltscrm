using VoltsCRM.Application.Common.Mappings;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Customers;

public static class CustomerMapping
{
    public static PersonalInfoDto ToDto(this PersonalInfo p) =>
        new(p.FirstName, p.LastName, p.Gender.ToString(), p.Phone, p.Email, p.FullName);

    public static ServiceLocationDto ToDto(this ServiceLocation s) =>
        new(s.Id, s.Label, s.Location.ToDto(), s.ActiveSubscriptionId, s.IsActive);

    public static CustomerListItemDto ToListItem(this Customer c) =>
        new(c.Id, c.AccountNumber, c.PersonalInfo.FullName, c.PersonalInfo.Phone,
            c.Status.ToString(), c.Location.Address.City);

    public static CustomerDto ToDto(this Customer c) =>
        new(c.Id, c.AccountNumber, c.PersonalInfo.ToDto(), c.Location.ToDto(),
            c.Status.ToString(),
            c.ServiceLocations.Select(s => s.ToDto()).ToList(), c.CreatedAt, c.UpdatedAt);

    // --- input → value object ---
    internal static PersonalInfo ToPersonalInfo(PersonalInfoInput i) =>
        new(i.FirstName, i.LastName, Enum.Parse<Gender>(i.Gender, true), i.Phone, i.Email);
}

using VoltsCRM.Application.Common.Models;

namespace VoltsCRM.Application.Features.Customers;

public sealed record PersonalInfoDto(
    string FirstName, string LastName, string Gender, string Phone, string? Email, string FullName);

public sealed record ServiceLocationDto(
    Guid Id, string Label, LocationDto Location, Guid? ActiveSubscriptionId, bool IsActive);

public sealed record CustomerListItemDto(
    Guid Id, string AccountNumber, string FullName, string Phone, string Status, string City);

public sealed record CustomerDto(
    Guid Id,
    string AccountNumber,
    PersonalInfoDto PersonalInfo,
    LocationDto Location,
    string Status,
    IReadOnlyList<ServiceLocationDto> ServiceLocations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

// --- request inputs (nested value objects) ---
public sealed record PersonalInfoInput(string FirstName, string LastName, string Gender, string Phone, string? Email);

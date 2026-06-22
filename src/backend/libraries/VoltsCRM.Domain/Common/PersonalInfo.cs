using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Domain.Common;

public sealed class PersonalInfo : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";
    public Gender Gender { get; }
    public string Phone { get; }
    public string? Email { get; }

    public PersonalInfo(string firstName, string lastName, Gender gender, string phone, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone number is required.", nameof(phone));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Gender = gender;
        Phone = phone.Trim();
        Email = email?.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
        yield return Gender;
        yield return Phone;
        yield return Email;
    }
}

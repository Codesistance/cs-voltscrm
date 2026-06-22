using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Tests for the <c>GET /api/portal/me/profile</c> endpoint, verifying:
/// - Customer tokens return their own profile (200).
/// - Admin/Agent tokens are forbidden (403).
/// - Customer tokens with null CustomerId are forbidden (403).
/// </summary>
[Collection("SharedTestContainers")]
public class PortalProfileTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PortalProfileTests);

    private const string ProfileEndpoint = "/api/portal/me/profile";

    public sealed record SeededCustomer(string UserId, Guid CustomerId, string AccountNumber);

    private async Task<SeededCustomer> CreateCustomerUserAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var db = sp.GetRequiredService<AppDbContext>();

        var accountNumber = $"CUST-{Guid.NewGuid():N}"[..16];
        var personalInfo = new PersonalInfo("John", "Doe", Gender.Male, "+1234567890", "john.doe@example.com");
        var address = new Address("123 Main St", "Springfield", "IL", "USA");
        var location = new Location(address);
        var customer = new Customer(accountNumber, personalInfo, location);

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var email = $"customer_{Guid.NewGuid():N}@voltscrm.local";
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "John",
            LastName = "Doe",
            UserType = UserType.Customer,
            CustomerId = customer.Id,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, TestUsers.DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create test customer user: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return new SeededCustomer(user.Id, customer.Id, accountNumber);
    }

    private async Task<string> CreateCustomerUserWithNullCustomerIdAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();

        var email = $"orphan_customer_{Guid.NewGuid():N}@voltscrm.local";
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Orphan",
            LastName = "Customer",
            UserType = UserType.Customer,
            CustomerId = null, // No linked customer profile
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, TestUsers.DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create test orphan customer user: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    [Fact]
    public async Task Profile_CustomerToken_ReturnsOkWithProfile()
    {
        // Arrange
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await CreateCustomerUserAsync(scope.ServiceProvider);
        var token = TestTokenFactory.Create(seeded.UserId, UserType.Customer);

        // Act
        var response = await GetAsync(ProfileEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<PortalProfileDtoT>();
        Assert.NotNull(profile);
        Assert.Equal(seeded.AccountNumber, profile.AccountNumber);
        Assert.Equal("John Doe", profile.FullName);
        Assert.Equal("+1234567890", profile.Phone);
        Assert.Equal("john.doe@example.com", profile.Email);
        Assert.Equal("Active", profile.Status);
        Assert.NotNull(profile.Address);
        Assert.Equal("123 Main St", profile.Address.Street);
        Assert.Equal("Springfield", profile.Address.City);
        Assert.Equal("IL", profile.Address.Region);
        Assert.Equal("USA", profile.Address.Country);
    }

    [Fact]
    public async Task Profile_AdminToken_ReturnsForbidden()
    {
        // Arrange
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration);

        // Act
        var response = await GetAsync(ProfileEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AgentToken_ReturnsForbidden()
    {
        // Arrange
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Agent);

        // Act
        var response = await GetAsync(ProfileEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Profile_CustomerWithNullCustomerId_ReturnsForbidden()
    {
        // Arrange
        using var scope = Factory.CreateScopeForArrange();
        var userId = await CreateCustomerUserWithNullCustomerIdAsync(scope.ServiceProvider);
        var token = TestTokenFactory.Create(userId, UserType.Customer);

        // Act
        var response = await GetAsync(ProfileEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Minimal DTO for deserialization in tests.</summary>
    private sealed record PortalProfileDtoT(
        string AccountNumber,
        string FullName,
        string Phone,
        string? Email,
        string Status,
        AddressDto Address);
}

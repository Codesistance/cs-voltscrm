using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Entities.Organisation;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Identity;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// End-to-end self-service payment via the no-op "voltspayments" gateway: a customer lists available
/// gateways and initiates a payment that completes inline, reducing the invoice balance. Also verifies
/// the "implemented ∩ visible" rule and cross-type authorization.
/// </summary>
[Collection("SharedTestContainers")]
public class PortalPaymentTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PortalPaymentTests);

    private const string GatewaysEndpoint = "/api/portal/me/gateways";
    private const string PaymentsEndpoint = "/api/portal/me/payments";

    private sealed record Seeded(string CustomerUserId, Guid CustomerId, Guid InvoiceId, decimal Balance);

    private async Task<Seeded> SeedCustomerWithInvoiceAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();

        var customer = new Customer(
            $"PAY-{Guid.NewGuid():N}"[..16],
            new PersonalInfo("Pay", "Tester", Gender.Male, "+1234567890", "pay@test.local"),
            new Location(new Address("1 Pay St", "PayCity", "PC", "PayLand")));
        db.Customers.Add(customer);

        var plan = new ServicePlan(
            $"PLN-{Guid.NewGuid():N}"[..10], "Pay Plan", BillingType.Postpaid, BillingCycle.Monthly,
            new Money(500m, "KES"), "Plan for payment tests");
        db.ServicePlans.Add(plan);

        var sub = new CustomerSubscription(customer.Id, plan.Id, BillingType.Postpaid, DateTimeOffset.UtcNow.AddMonths(-1));
        sub.Activate();
        db.CustomerSubscriptions.Add(sub);

        var invoice = new Invoice(sub.Id, customer.Id, DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month,
            grossAmount: 500m, dueDate: DateTimeOffset.UtcNow.AddDays(10), currency: "KES");
        invoice.AddLineItem("Monthly charge", 500m);
        db.Invoices.Add(invoice);

        var userManager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var email = $"pay_{Guid.NewGuid():N}@voltscrm.local";
        var user = new AppUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FirstName = "Pay", LastName = "Tester", UserType = UserType.Customer,
            CustomerId = customer.Id, IsActive = true,
        };
        var result = await userManager.CreateAsync(user, TestUsers.DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create customer user: " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await db.SaveChangesAsync();
        return new Seeded(user.Id, customer.Id, invoice.Id, 500m);
    }

    [Fact]
    public async Task Gateways_CustomerToken_IncludesVisibleImplementedVoltspayments()
    {
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedCustomerWithInvoiceAsync(scope.ServiceProvider);
        var token = TestTokenFactory.Create(seeded.CustomerUserId, UserType.Customer);

        var response = await GetAsync(GatewaysEndpoint, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var gateways = await response.Content.ReadFromJsonAsync<List<AvailableGatewayDtoT>>();
        Assert.NotNull(gateways);
        Assert.Contains(gateways, g => g.KeyName == "voltspayments");
    }

    [Fact]
    public async Task Gateways_ExcludesHiddenAndUnimplemented()
    {
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedCustomerWithInvoiceAsync(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Hide voltspayments, and add a visible-but-not-implemented gateway.
        var vp = await db.PaymentGatewayConfigs.FirstAsync(c => c.KeyName == "voltspayments", TestContext.Current.CancellationToken);
        vp.SetVisibility(false);
        db.PaymentGatewayConfigs.Add(new PaymentGatewayConfig("ghostpay", "Ghost Pay", visibility: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var token = TestTokenFactory.Create(seeded.CustomerUserId, UserType.Customer);
        var response = await GetAsync(GatewaysEndpoint, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var gateways = await response.Content.ReadFromJsonAsync<List<AvailableGatewayDtoT>>();
        Assert.NotNull(gateways);
        Assert.DoesNotContain(gateways, g => g.KeyName == "voltspayments"); // hidden
        Assert.DoesNotContain(gateways, g => g.KeyName == "ghostpay");      // not implemented
    }

    [Fact]
    public async Task InitiatePayment_VoltspaymentsForInvoice_CompletesAndClearsBalance()
    {
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedCustomerWithInvoiceAsync(scope.ServiceProvider);
        var token = TestTokenFactory.Create(seeded.CustomerUserId, UserType.Customer);

        var response = await PostAsync(PaymentsEndpoint,
            new { invoiceId = seeded.InvoiceId, gatewayKey = "voltspayments" }, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InitiateResultDtoT>();
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.NotEqual(Guid.Empty, result.PaymentId);

        // Invoice fully paid → balance cleared.
        using var verifyScope = Factory.CreateScopeForArrange();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstAsync(i => i.Id == seeded.InvoiceId, TestContext.Current.CancellationToken);
        Assert.Equal(0m, invoice.Balance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public async Task InitiatePayment_HiddenGateway_ReturnsValidationError()
    {
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedCustomerWithInvoiceAsync(scope.ServiceProvider);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vp = await db.PaymentGatewayConfigs.FirstAsync(c => c.KeyName == "voltspayments", TestContext.Current.CancellationToken);
        vp.SetVisibility(false);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var token = TestTokenFactory.Create(seeded.CustomerUserId, UserType.Customer);
        var response = await PostAsync(PaymentsEndpoint,
            new { invoiceId = seeded.InvoiceId, gatewayKey = "voltspayments" }, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InitiatePayment_AdminToken_ReturnsForbidden()
    {
        var token = TestTokenFactory.Create(Guid.NewGuid().ToString(), UserType.Administration);
        var response = await PostAsync(PaymentsEndpoint, new { amount = 100m, gatewayKey = "voltspayments" }, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record AvailableGatewayDtoT(string KeyName, string DisplayName);
    private sealed record InitiateResultDtoT(Guid PaymentId, string Status, string? CheckoutUrl);
}

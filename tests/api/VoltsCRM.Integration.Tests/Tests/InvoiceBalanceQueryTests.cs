using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Tests for endpoints that query Invoice.Balance / AmountDue server-side,
/// verifying the fix for the unmapped computed property issue (A9 in TODO-WORK.md).
/// Seeds invoices with partial payments and asserts correct figures are returned.
/// </summary>
[Collection("SharedTestContainers")]
public class InvoiceBalanceQueryTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(InvoiceBalanceQueryTests);

    private const string DashboardSummaryEndpoint = "/api/reports/dashboard-summary";
    private const string AgingReportEndpoint = "/api/reports/aging";
    private const string PortalSummaryEndpoint = "/api/portal/me/summary";

    private sealed record SeededData(
        Guid CustomerId,
        string CustomerUserId,
        Guid SubscriptionId,
        Guid InvoiceId,
        decimal GrossAmount,
        decimal DiscountAmount,
        decimal AmountPaid);

    private async Task<SeededData> SeedInvoiceWithPartialPaymentAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();

        // Create customer
        var accountNumber = $"TEST-{Guid.NewGuid():N}"[..16];
        var personalInfo = new PersonalInfo("Balance", "Tester", Gender.Male, "+1234567890", "balance@test.local");
        var address = new Address("123 Test St", "TestCity", "TC", "TestCountry");
        var location = new Location(address);
        var customer = new Customer(accountNumber, personalInfo, location);
        db.Customers.Add(customer);

        // Create service plan (required for subscription)
        var plan = new ServicePlan(
            $"TEST-{Guid.NewGuid():N}"[..10],
            "Test Plan",
            BillingType.Postpaid,
            BillingCycle.Monthly,
            new Money(1000m, "KES"),
            "Test plan for balance queries");
        db.ServicePlans.Add(plan);

        // Create subscription
        var subscription = new CustomerSubscription(
            customer.Id,
            plan.Id,
            BillingType.Postpaid,
            DateTimeOffset.UtcNow.AddMonths(-1));
        subscription.Activate();
        db.CustomerSubscriptions.Add(subscription);

        // Create invoice with partial payment
        var dueDate = DateTimeOffset.UtcNow.AddDays(-15); // Past due for aging report
        var invoice = new Invoice(
            subscription.Id,
            customer.Id,
            DateTimeOffset.UtcNow.Year,
            DateTimeOffset.UtcNow.Month,
            grossAmount: 1000m,
            dueDate: dueDate,
            currency: "KES");

        invoice.AddLineItem("Monthly charge", 1000m);
        invoice.AddLineItem("Early-bird discount", -100m, isDiscount: true);
        invoice.RecordPayment(400m); // Partial payment: Balance = 1000 - 100 - 400 = 500

        db.Invoices.Add(invoice);

        // Create portal user for this customer
        var userManager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<VoltsCRM.Infrastructure.Identity.AppUser>>();
        var email = $"portal_{Guid.NewGuid():N}@voltscrm.local";
        var user = new VoltsCRM.Infrastructure.Identity.AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Balance",
            LastName = "Tester",
            UserType = UserType.Customer,
            CustomerId = customer.Id,
            IsActive = true,
        };

        var result = await userManager.CreateAsync(user, TestUsers.DefaultPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create test portal user: " +
                string.Join("; ", result.Errors.Select(e => e.Description)));

        await db.SaveChangesAsync();

        return new SeededData(
            customer.Id,
            user.Id,
            subscription.Id,
            invoice.Id,
            GrossAmount: 1000m,
            DiscountAmount: 100m,
            AmountPaid: 400m);
    }

    [Fact]
    public async Task DashboardSummary_ReturnsOkWithCorrectOutstandingBalance()
    {
        // Arrange
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedInvoiceWithPartialPaymentAsync(scope.ServiceProvider);
        var expectedBalance = seeded.GrossAmount - seeded.DiscountAmount - seeded.AmountPaid; // 500

        // Admin token required for reports
        var token = TestTokenFactory.Create(
            Guid.NewGuid().ToString(),
            UserType.Administration,
            ["reports.view"]);

        // Act
        var response = await GetAsync(DashboardSummaryEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDtoT>();
        Assert.NotNull(summary);
        Assert.True(summary.OutstandingBalance.Amount >= expectedBalance,
            $"Outstanding balance {summary.OutstandingBalance.Amount} should be >= {expectedBalance}");
        Assert.True(summary.OverdueInvoices >= 1,
            $"Overdue invoices {summary.OverdueInvoices} should be >= 1");
    }

    [Fact]
    public async Task AgingReport_ReturnsOkWithCorrectBuckets()
    {
        // Arrange
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedInvoiceWithPartialPaymentAsync(scope.ServiceProvider);
        var expectedBalance = seeded.GrossAmount - seeded.DiscountAmount - seeded.AmountPaid; // 500

        var token = TestTokenFactory.Create(
            Guid.NewGuid().ToString(),
            UserType.Administration,
            ["reports.view"]);

        // Act
        var response = await GetAsync(AgingReportEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var report = await response.Content.ReadFromJsonAsync<AgingReportDtoT>();
        Assert.NotNull(report);
        Assert.NotNull(report.Buckets);
        Assert.True(report.Buckets.Count >= 1, "Should have at least one aging bucket");

        // The seeded invoice is 15 days past due, so it should be in the 0-30 bucket
        var totalBalance = report.Buckets.Sum(b => b.TotalBalance.Amount);
        Assert.True(totalBalance >= expectedBalance,
            $"Total aging balance {totalBalance} should be >= {expectedBalance}");
    }

    [Fact]
    public async Task PortalSummary_ReturnsOkWithCorrectOutstandingBalance()
    {
        // Arrange
        using var scope = Factory.CreateScopeForArrange();
        var seeded = await SeedInvoiceWithPartialPaymentAsync(scope.ServiceProvider);
        var expectedBalance = seeded.GrossAmount - seeded.DiscountAmount - seeded.AmountPaid; // 500

        // Customer token for portal endpoints
        var token = TestTokenFactory.Create(
            seeded.CustomerUserId,
            UserType.Customer);

        // Act
        var response = await GetAsync(PortalSummaryEndpoint, token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<PortalSummaryDtoT>();
        Assert.NotNull(summary);
        Assert.Equal(expectedBalance, summary.OutstandingBalance.Amount);
        Assert.True(summary.PendingInvoices >= 1,
            $"Pending invoices {summary.PendingInvoices} should be >= 1");
    }

    // Minimal DTOs for test deserialization
    private sealed record MoneyDtoT(decimal Amount, string Currency);

    private sealed record DashboardSummaryDtoT(
        int ActiveCustomers,
        MoneyDtoT OutstandingBalance,
        MoneyDtoT CollectionsMtd,
        int OverdueInvoices);

    private sealed record AgingBucketDtoT(
        string Bucket,
        int InvoiceCount,
        MoneyDtoT TotalBalance);

    private sealed record AgingReportDtoT(List<AgingBucketDtoT> Buckets);

    private sealed record PortalSummaryDtoT(
        MoneyDtoT OutstandingBalance,
        int PendingInvoices,
        int ActiveSubscriptions,
        MoneyDtoT PaidThisMonth);
}

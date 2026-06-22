using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Entities.Crm;
using VoltsCRM.Domain.Enums;
using VoltsCRM.Infrastructure.Integrations;
using VoltsCRM.Infrastructure.Persistence;
using VoltsCRM.Integration.Tests.Helpers;

namespace VoltsCRM.Integration.Tests.Tests;

/// <summary>
/// Payment webhook endpoint: valid signatures complete a pending payment, tampered signatures are
/// rejected (401), and a route bound to a stub adapter is unreachable (404). Signature is the auth —
/// the endpoint is anonymous.
/// </summary>
[Collection("SharedTestContainers")]
public class PaymentWebhookTests(SharedTestContainersFixture fixture) : IntegrationTestBase(fixture)
{
    protected override string TestName => nameof(PaymentWebhookTests);

    private const string Secret = CustomWebApplicationFactory.TestVoltspaymentsWebhookSecret;

    private async Task<(Guid PaymentId, string Reference)> SeedPendingPaymentAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var customer = new Customer(
            $"WH-{Guid.NewGuid():N}"[..16],
            new PersonalInfo("Hook", "Tester", Gender.Male, "+1234567890", "hook@test.local"),
            new Location(new Address("1 Hook St", "HookCity", "HC", "HookLand")));
        db.Customers.Add(customer);

        var reference = $"VP-{Guid.NewGuid():N}".ToUpperInvariant();
        var payment = new Payment(customer.Id, 250m, "KES", PaymentMethod.Online, PaymentChannel.CustomerPortal,
            DateTimeOffset.UtcNow, platformProvider: "voltspayments", platformReference: reference);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return (payment.Id, reference);
    }

    private async Task<HttpResponseMessage> PostWebhookAsync(string gatewayKey, string body, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webhooks/payments/{gatewayKey}")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("X-Signature", signature);
        return await Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Webhook_ValidSignature_CompletesPayment()
    {
        using var scope = Factory.CreateScopeForArrange();
        var (paymentId, reference) = await SeedPendingPaymentAsync(scope.ServiceProvider);

        var body = $"{{\"transactionReference\":\"{reference}\",\"status\":\"Completed\"}}";
        var sig = WebhookSignature.Compute(body, Secret);

        var response = await PostWebhookAsync("voltspayments", body, sig);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verify = Factory.CreateScopeForArrange();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await db.Payments.Where(p => p.Id == paymentId).Select(p => p.Status)
            .FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatus.Completed, status);
    }

    [Fact]
    public async Task Webhook_TamperedSignature_ReturnsUnauthorized()
    {
        using var scope = Factory.CreateScopeForArrange();
        var (paymentId, reference) = await SeedPendingPaymentAsync(scope.ServiceProvider);

        var body = $"{{\"transactionReference\":\"{reference}\",\"status\":\"Completed\"}}";
        var sig = WebhookSignature.Compute(body, Secret);

        // Tamper with the body after signing.
        var tamperedBody = body.Replace("Completed", "Failed");
        var response = await PostWebhookAsync("voltspayments", tamperedBody, sig);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var verify = Factory.CreateScopeForArrange();
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await db.Payments.Where(p => p.Id == paymentId).Select(p => p.Status)
            .FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PaymentStatus.Pending, status); // unchanged
    }

    [Fact]
    public async Task Webhook_StubGateway_ReturnsNotFound()
    {
        var body = "{\"transactionReference\":\"x\",\"status\":\"Completed\"}";
        var sig = WebhookSignature.Compute(body, Secret);
        var response = await PostWebhookAsync("mpesa", body, sig); // mpesa is still a stub adapter
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_UnknownGateway_ReturnsNotFound()
    {
        var body = "{\"transactionReference\":\"x\",\"status\":\"Completed\"}";
        var sig = WebhookSignature.Compute(body, Secret);
        var response = await PostWebhookAsync("nope", body, sig);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

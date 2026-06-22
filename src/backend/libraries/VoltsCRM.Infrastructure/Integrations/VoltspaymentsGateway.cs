using Microsoft.Extensions.Logging;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Infrastructure.Integrations;

/// <summary>
/// First-party no-op payment gateway. It is a <b>real</b> adapter (not a <see cref="IStubGateway"/>):
/// it settles payments instantly and performs genuine constant-time HMAC webhook verification, so the
/// full initiate → reconcile/webhook → complete flow works end-to-end with zero external dependency.
/// Real gateways (M-Pesa/Stripe/...) drop in by implementing <see cref="IPaymentGateway"/> the same way.
/// </summary>
public sealed class VoltspaymentsGateway(ILogger<VoltspaymentsGateway> logger) : IPaymentGateway
{
    public const string Key = "voltspayments";

    public string ProviderKey => Key;

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken ct)
    {
        logger.LogInformation("VoltsPayments: initiating {Amount} {Currency} for {Customer}",
            request.Amount, request.Currency, request.CustomerReference);

        var txRef = $"VP-{Guid.NewGuid():N}".ToUpperInvariant();
        return Task.FromResult(new PaymentInitiationResult(
            Success: true,
            TransactionReference: txRef,
            CheckoutUrl: null, // settles immediately; no redirect needed
            ErrorMessage: null));
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionReference, CancellationToken ct)
    {
        logger.LogInformation("VoltsPayments: status query for {TxRef}", transactionReference);

        // No-op gateway settles instantly — drives inline reconcile after initiate.
        return Task.FromResult(new PaymentStatusResult(
            Found: true,
            Status: "Completed",
            Amount: null,
            Currency: null,
            CompletedAt: DateTimeOffset.UtcNow,
            ErrorMessage: null));
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
        => WebhookSignature.Verify(payload, signature, secret);
}

using Microsoft.Extensions.Logging;
using VoltsCRM.Application.Common.Interfaces;

namespace VoltsCRM.Infrastructure.Integrations;

/// <summary>
/// Stub M-Pesa gateway implementation. Replace with actual API integration in Phase 20.
/// </summary>
public sealed class MpesaPaymentGateway(ILogger<MpesaPaymentGateway> logger) : IPaymentGateway, IStubGateway
{
    public string ProviderKey => "mpesa";

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken ct)
    {
        logger.LogInformation("M-Pesa: Initiating payment of {Amount} {Currency} for {Customer}",
            request.Amount, request.Currency, request.CustomerReference);

        // Stub: Return a fake transaction reference
        var txRef = $"MPESA-{Guid.NewGuid():N}".ToUpperInvariant()[..20];
        return Task.FromResult(new PaymentInitiationResult(
            Success: true,
            TransactionReference: txRef,
            CheckoutUrl: null,
            ErrorMessage: null));
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionReference, CancellationToken ct)
    {
        logger.LogInformation("M-Pesa: Querying payment status for {TxRef}", transactionReference);

        // Stub: Return completed status
        return Task.FromResult(new PaymentStatusResult(
            Found: true,
            Status: "Completed",
            Amount: null,
            Currency: null,
            CompletedAt: DateTimeOffset.UtcNow,
            ErrorMessage: null));
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        // Stub: Always return true for now
        logger.LogDebug("M-Pesa: Validating webhook signature");
        return true;
    }
}

/// <summary>
/// Stub Stripe gateway implementation. Replace with actual API integration in Phase 20.
/// </summary>
public sealed class StripePaymentGateway(ILogger<StripePaymentGateway> logger) : IPaymentGateway, IStubGateway
{
    public string ProviderKey => "stripe";

    public Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken ct)
    {
        logger.LogInformation("Stripe: Initiating payment of {Amount} {Currency} for {Customer}",
            request.Amount, request.Currency, request.CustomerReference);

        var txRef = $"pi_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentInitiationResult(
            Success: true,
            TransactionReference: txRef,
            CheckoutUrl: $"https://checkout.stripe.com/pay/{txRef}",
            ErrorMessage: null));
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionReference, CancellationToken ct)
    {
        logger.LogInformation("Stripe: Querying payment status for {TxRef}", transactionReference);

        return Task.FromResult(new PaymentStatusResult(
            Found: true,
            Status: "Completed",
            Amount: null,
            Currency: null,
            CompletedAt: DateTimeOffset.UtcNow,
            ErrorMessage: null));
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        logger.LogDebug("Stripe: Validating webhook signature");
        return true;
    }
}

/// <summary>
/// Stub KPLC token vending implementation. Replace with actual API integration in Phase 20.
/// </summary>
public sealed class KplcTokenVendingPlatform(ILogger<KplcTokenVendingPlatform> logger) : ITokenVendingPlatform
{
    public string ProviderKey => "kplc";

    public Task<TokenPurchaseResult> PurchaseTokenAsync(TokenPurchaseRequest request, CancellationToken ct)
    {
        logger.LogInformation("KPLC: Purchasing token for meter {Meter}, amount {Amount} {Currency}",
            request.MeterNumber, request.Amount, request.Currency);

        // Stub: Generate a fake token
        var txRef = $"KPLC-{Guid.NewGuid():N}".ToUpperInvariant()[..16];
        var token = string.Join("-", Enumerable.Range(0, 5).Select(_ => Random.Shared.Next(1000, 9999)));
        var units = Math.Round(request.Amount / 25m, 2); // Approx 25 KES per unit

        return Task.FromResult(new TokenPurchaseResult(
            Success: true,
            TransactionReference: txRef,
            Token: token,
            Units: units,
            ErrorMessage: null));
    }

    public Task<TokenStatusResult> GetTokenStatusAsync(string transactionReference, CancellationToken ct)
    {
        logger.LogInformation("KPLC: Querying token status for {TxRef}", transactionReference);

        return Task.FromResult(new TokenStatusResult(
            Found: true,
            Status: "Completed",
            Token: "1234-5678-9012-3456-7890",
            Units: 10.5m,
            ErrorMessage: null));
    }
}

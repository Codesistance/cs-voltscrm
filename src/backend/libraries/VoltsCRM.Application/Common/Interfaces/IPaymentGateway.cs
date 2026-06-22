namespace VoltsCRM.Application.Common.Interfaces;

/// <summary>
/// Abstraction for payment gateway integrations. Implementations are registered as keyed services
/// using the provider name as the key (e.g., "mpesa", "stripe").
/// </summary>
public interface IPaymentGateway
{
    /// <summary>The provider key (e.g., "mpesa", "stripe").</summary>
    string ProviderKey { get; }

    /// <summary>Initiates a payment request.</summary>
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken ct);

    /// <summary>Queries the status of a payment.</summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionReference, CancellationToken ct);

    /// <summary>Validates a webhook signature.</summary>
    bool ValidateWebhookSignature(string payload, string signature, string secret);
}

/// <summary>
/// Marker for gateway adapters that are NOT production-ready (signature validation is a no-op stub).
/// A gateway implementing this must never be exposed: the startup guard fails closed if a config row
/// with Visibility=true maps to a stub adapter, and the webhook endpoint refuses to bind to one.
/// </summary>
public interface IStubGateway;

/// <summary>
/// Knows which payment gateway adapters are actually implemented (registered as keyed services) and
/// resolves a gateway instance by key. Backs the "implemented ∩ visible" rule.
/// </summary>
public interface IPaymentGatewayCatalog
{
    /// <summary>Provider keys for every registered <see cref="IPaymentGateway"/> adapter.</summary>
    IReadOnlyCollection<string> ImplementedKeys { get; }

    /// <summary>Resolves the adapter for <paramref name="keyName"/>, or null if none is registered.</summary>
    IPaymentGateway? Resolve(string keyName);
}

public sealed record PaymentInitiationRequest(
    decimal Amount,
    string Currency,
    string CustomerReference,
    string? PhoneNumber,
    string? Description,
    string CallbackUrl);

public sealed record PaymentInitiationResult(
    bool Success,
    string? TransactionReference,
    string? CheckoutUrl,
    string? ErrorMessage);

public sealed record PaymentStatusResult(
    bool Found,
    string Status, // Pending, Completed, Failed
    decimal? Amount,
    string? Currency,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);

/// <summary>
/// Abstraction for token vending platform integrations. Implementations are registered as keyed services
/// using the provider name as the key (e.g., "kplc", "umeme").
/// </summary>
public interface ITokenVendingPlatform
{
    /// <summary>The provider key (e.g., "kplc", "umeme").</summary>
    string ProviderKey { get; }

    /// <summary>Purchases a token for the given meter.</summary>
    Task<TokenPurchaseResult> PurchaseTokenAsync(TokenPurchaseRequest request, CancellationToken ct);

    /// <summary>Queries the status of a token purchase.</summary>
    Task<TokenStatusResult> GetTokenStatusAsync(string transactionReference, CancellationToken ct);
}

public sealed record TokenPurchaseRequest(
    string MeterNumber,
    decimal Amount,
    string Currency,
    string CustomerReference);

public sealed record TokenPurchaseResult(
    bool Success,
    string? TransactionReference,
    string? Token,
    decimal? Units,
    string? ErrorMessage);

public sealed record TokenStatusResult(
    bool Found,
    string Status, // Pending, Completed, Failed
    string? Token,
    decimal? Units,
    string? ErrorMessage);

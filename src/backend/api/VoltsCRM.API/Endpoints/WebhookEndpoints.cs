using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Features.Payments;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.API.Endpoints;

/// <summary>
/// Inbound payment-gateway webhooks. Anonymous — the request is authenticated by its HMAC signature,
/// not a bearer token. A gateway whose adapter is still a stub (always-true validation) can never be
/// reached here (404), mirroring the startup guard that forbids exposing a stub.
/// </summary>
public static class WebhookEndpoints
{
    /// <summary>Minimal generic callback contract; real adapters parse their own provider payload.</summary>
    private sealed record WebhookCallback(string? TransactionReference, string? Status);

    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks/payments")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .RequireRateLimiting("webhook");

        group.MapPost("/{gatewayKey}", HandlePaymentWebhookAsync);
        return app;
    }

    private static async Task<IResult> HandlePaymentWebhookAsync(
        string gatewayKey,
        HttpRequest httpRequest,
        IPaymentGatewayCatalog catalog,
        IAppDbContext db,
        ISender sender,
        CancellationToken ct)
    {
        var gateway = catalog.Resolve(gatewayKey);
        // Unknown gateway, or one still backed by a stub adapter → not a bindable webhook target.
        if (gateway is null || gateway is IStubGateway)
            return TypedResults.NotFound();

        // Read the raw body exactly as signed.
        httpRequest.EnableBuffering();
        string payload;
        using (var reader = new StreamReader(httpRequest.Body, leaveOpen: true))
            payload = await reader.ReadToEndAsync(ct);

        var signature = httpRequest.Headers["X-Signature"].ToString();

        var config = await db.PaymentGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.KeyName == gatewayKey, ct);
        if (config is null || !config.Data.TryGetValue("webhookSecret", out var secret) || string.IsNullOrEmpty(secret))
            return TypedResults.NotFound();

        if (!gateway.ValidateWebhookSignature(payload, signature, secret))
            return TypedResults.Unauthorized();

        WebhookCallback? callback;
        try { callback = JsonSerializer.Deserialize<WebhookCallback>(payload, JsonSerializerOptions.Web); }
        catch (JsonException) { return TypedResults.BadRequest(); }

        if (callback is null || string.IsNullOrWhiteSpace(callback.TransactionReference))
            return TypedResults.BadRequest();

        var payment = await db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlatformReference == callback.TransactionReference, ct);
        if (payment is null)
            return TypedResults.NotFound();

        // Idempotent: only act on a payment still pending.
        if (payment.Status == PaymentStatus.Pending)
        {
            if (string.Equals(callback.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                await sender.Send(new CompletePaymentCommand(payment.Id), ct);
            else if (string.Equals(callback.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                await sender.Send(new FailPaymentCommand(payment.Id), ct);
        }

        return TypedResults.Ok();
    }
}

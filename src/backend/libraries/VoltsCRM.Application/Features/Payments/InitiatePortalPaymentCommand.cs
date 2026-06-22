using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Common;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Payments;

/// <summary>
/// Customer-initiated (self-service) payment. Creates a Pending payment, calls the chosen gateway to
/// initiate, then immediately reconciles status — so an instant gateway (voltspayments) lands Completed
/// in one call, while async gateways stay Pending until their webhook arrives.
/// </summary>
public sealed record InitiatePortalPaymentCommand(
    Guid CustomerId,
    Guid? InvoiceId,
    decimal? Amount,
    string GatewayKey) : IRequest<InitiatePaymentResultDto>;

public sealed record InitiatePaymentResultDto(Guid PaymentId, string Status, string? CheckoutUrl);

public sealed class InitiatePortalPaymentValidator : AbstractValidator<InitiatePortalPaymentCommand>
{
    public InitiatePortalPaymentValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.GatewayKey).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.InvoiceId.HasValue || x.Amount is > 0)
            .WithMessage("Provide an invoice to pay or a positive amount.");
    }
}

public sealed class InitiatePortalPaymentHandler(
    IAppDbContext db,
    IPaymentGatewayCatalog catalog,
    ISender sender) : IRequestHandler<InitiatePortalPaymentCommand, InitiatePaymentResultDto>
{
    public async Task<InitiatePaymentResultDto> Handle(InitiatePortalPaymentCommand cmd, CancellationToken ct)
    {
        // Gateway must be both visible (offered) and implemented (an adapter exists).
        var config = await db.PaymentGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.KeyName == cmd.GatewayKey, ct);
        var gateway = catalog.Resolve(cmd.GatewayKey);
        if (config is null || !config.Visibility || gateway is null)
            throw new ValidationException([new ValidationFailure(nameof(cmd.GatewayKey),
                $"Payment gateway '{cmd.GatewayKey}' is not available.")]);

        // Resolve amount + currency. An invoice pays its outstanding balance; otherwise an explicit amount.
        decimal amount;
        string currency;
        Invoice? invoice = null;
        if (cmd.InvoiceId.HasValue)
        {
            invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == cmd.InvoiceId.Value, ct)
                ?? throw new NotFoundException(nameof(Invoice), cmd.InvoiceId.Value);
            if (invoice.CustomerId != cmd.CustomerId)
                throw new ValidationException([new ValidationFailure(nameof(cmd.InvoiceId), "Invoice does not belong to the customer.")]);
            if (invoice.Status == InvoiceStatus.Paid || invoice.Balance <= 0)
                throw new ValidationException([new ValidationFailure(nameof(cmd.InvoiceId), "Invoice has no outstanding balance.")]);

            amount = invoice.Balance;
            currency = invoice.Currency;
        }
        else
        {
            amount = cmd.Amount!.Value;
            currency = Money.DefaultCurrency;
        }

        // Initiate with the gateway first so we can persist its transaction reference.
        var initiation = await gateway.InitiatePaymentAsync(
            new PaymentInitiationRequest(
                amount, currency,
                CustomerReference: cmd.CustomerId.ToString(),
                PhoneNumber: null,
                Description: invoice is not null ? $"Invoice {invoice.Id}" : "Account payment",
                CallbackUrl: $"/api/webhooks/payments/{cmd.GatewayKey}"),
            ct);

        if (!initiation.Success)
            throw new ValidationException([new ValidationFailure(nameof(cmd.GatewayKey),
                initiation.ErrorMessage ?? "Payment gateway failed to initiate the payment.")]);

        var payment = new Payment(cmd.CustomerId, amount, currency,
            PaymentMethod.Online, PaymentChannel.CustomerPortal, DateTimeOffset.UtcNow,
            platformProvider: cmd.GatewayKey, platformReference: initiation.TransactionReference);

        if (invoice is not null)
            payment.AllocateToInvoice(invoice.Id, amount);

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        // Inline reconcile: instant gateways report Completed immediately; complete via the shared path.
        var status = await gateway.GetPaymentStatusAsync(initiation.TransactionReference!, ct);
        if (string.Equals(status.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            await sender.Send(new CompletePaymentCommand(payment.Id), ct);
        }
        else if (string.Equals(status.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            await sender.Send(new FailPaymentCommand(payment.Id), ct);
        }

        var finalStatus = await db.Payments.AsNoTracking()
            .Where(p => p.Id == payment.Id)
            .Select(p => p.Status)
            .FirstAsync(ct);

        return new InitiatePaymentResultDto(payment.Id, finalStatus.ToString(), initiation.CheckoutUrl);
    }
}

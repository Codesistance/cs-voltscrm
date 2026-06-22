using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Payments;

/// <summary>Input for allocating a payment to invoices or installments.</summary>
public sealed record PaymentAllocationInput(Guid? InvoiceId, Guid? InstallmentId, decimal Amount);

public sealed record RecordPaymentCommand(
    Guid CustomerId,
    decimal Amount,
    string Currency,
    string Method,
    string Channel,
    DateTimeOffset? PaymentDate,
    string? PlatformProvider,
    string? PlatformReference,
    string? CollectedByAgentId,
    IReadOnlyList<PaymentAllocationInput>? Allocations = null,
    bool AutoComplete = false) : IRequest<PaymentDto>;

public sealed class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Method).Must(m => Enum.TryParse<PaymentMethod>(m, true, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Channel).Must(c => Enum.TryParse<PaymentChannel>(c, true, out _)).WithMessage("Invalid payment channel.");
        RuleFor(x => x.PlatformProvider).MaximumLength(100);
        RuleFor(x => x.PlatformReference).MaximumLength(100);

        RuleForEach(x => x.Allocations).ChildRules(alloc =>
        {
            alloc.RuleFor(a => a.Amount).GreaterThan(0).WithMessage("Allocation amount must be positive.");
            alloc.RuleFor(a => a)
                .Must(a => a.InvoiceId.HasValue || a.InstallmentId.HasValue)
                .WithMessage("Allocation must target an invoice or installment.");
            alloc.RuleFor(a => a)
                .Must(a => !(a.InvoiceId.HasValue && a.InstallmentId.HasValue))
                .WithMessage("Allocation cannot target both invoice and installment.");
        });

        RuleFor(x => x)
            .Must(x => x.Allocations == null || x.Allocations.Sum(a => a.Amount) <= x.Amount)
            .WithMessage("Total allocation amount cannot exceed payment amount.");
    }
}

public sealed class RecordPaymentHandler(IAppDbContext db) : IRequestHandler<RecordPaymentCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(RecordPaymentCommand cmd, CancellationToken ct)
    {
        if (!await db.Customers.AnyAsync(c => c.Id == cmd.CustomerId, ct))
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(RecordPaymentCommand.CustomerId), "Customer does not exist."),
            });

        var method = Enum.Parse<PaymentMethod>(cmd.Method, true);
        var channel = Enum.Parse<PaymentChannel>(cmd.Channel, true);
        var paymentDate = cmd.PaymentDate ?? DateTimeOffset.UtcNow;

        var payment = new Payment(cmd.CustomerId, cmd.Amount, cmd.Currency, method, channel, paymentDate,
            cmd.PlatformProvider, cmd.PlatformReference, cmd.CollectedByAgentId);

        // Apply single-payment scoped discount if available (B2)
        await ApplySinglePaymentDiscountAsync(payment, paymentDate, ct);

        // Store allocations on the payment (actual effects applied on completion)
        if (cmd.Allocations is { Count: > 0 })
        {
            await ValidateAllocationsAsync(cmd.CustomerId, cmd.Allocations, ct);
            foreach (var alloc in cmd.Allocations)
            {
                if (alloc.InvoiceId.HasValue)
                    payment.AllocateToInvoice(alloc.InvoiceId.Value, alloc.Amount);
                else if (alloc.InstallmentId.HasValue)
                    payment.AllocateToInstallment(alloc.InstallmentId.Value, alloc.Amount);
            }
        }

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        // Auto-complete for cash payments or explicit flag
        if (cmd.AutoComplete || method == PaymentMethod.Cash)
        {
            await PaymentCompletionHelper.ApplyCompletionEffectsAsync(db, payment, ct);
            await db.SaveChangesAsync(ct);
        }

        return await payment.ToDetailDtoAsync(db, ct);
    }

    private async Task ApplySinglePaymentDiscountAsync(Payment payment, DateTimeOffset paymentDate, CancellationToken ct)
    {
        // Find active single-payment discount grant for this customer
        var grant = await db.DiscountGrants
            .Where(g => g.CustomerId == payment.CustomerId
                     && g.Scope == DiscountScope.SinglePayment
                     && g.Status == DiscountGrantStatus.Active
                     && g.ValidFrom <= paymentDate
                     && (g.ValidUntil == null || g.ValidUntil >= paymentDate))
            .OrderByDescending(g => g.GrantedAt)
            .FirstOrDefaultAsync(ct);

        if (grant is null) return;

        var discountAmount = grant.CalculateDiscountAmount(payment.Amount);
        if (discountAmount > 0)
        {
            payment.ApplyDiscount(discountAmount, grant.Id);
            grant.MarkApplied(); // Marks as Applied if not recurring
        }
    }

    private async Task ValidateAllocationsAsync(Guid customerId, IReadOnlyList<PaymentAllocationInput> allocations, CancellationToken ct)
    {
        var errors = new List<ValidationFailure>();

        var invoiceIds = allocations.Where(a => a.InvoiceId.HasValue).Select(a => a.InvoiceId!.Value).Distinct().ToList();
        if (invoiceIds.Count > 0)
        {
            var invoices = await db.Invoices.AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .Select(i => new { i.Id, i.CustomerId, i.Status })
                .ToListAsync(ct);

            foreach (var invoiceId in invoiceIds)
            {
                var invoice = invoices.FirstOrDefault(i => i.Id == invoiceId);
                if (invoice == null)
                    errors.Add(new ValidationFailure("Allocations", $"Invoice {invoiceId} does not exist."));
                else if (invoice.CustomerId != customerId)
                    errors.Add(new ValidationFailure("Allocations", $"Invoice {invoiceId} does not belong to the customer."));
                else if (invoice.Status == InvoiceStatus.Paid)
                    errors.Add(new ValidationFailure("Allocations", $"Invoice {invoiceId} is already paid."));
            }
        }

        var installmentIds = allocations.Where(a => a.InstallmentId.HasValue).Select(a => a.InstallmentId!.Value).Distinct().ToList();
        if (installmentIds.Count > 0)
        {
            var installments = await db.Installments.AsNoTracking()
                .Include(i => i.InstallmentPlan)
                .Where(i => installmentIds.Contains(i.Id))
                .Select(i => new { i.Id, i.InstallmentPlan.CustomerId, i.Status })
                .ToListAsync(ct);

            foreach (var installmentId in installmentIds)
            {
                var installment = installments.FirstOrDefault(i => i.Id == installmentId);
                if (installment == null)
                    errors.Add(new ValidationFailure("Allocations", $"Installment {installmentId} does not exist."));
                else if (installment.CustomerId != customerId)
                    errors.Add(new ValidationFailure("Allocations", $"Installment {installmentId} does not belong to the customer."));
                else if (installment.Status == InstallmentStatus.Paid)
                    errors.Add(new ValidationFailure("Allocations", $"Installment {installmentId} is already paid."));
            }
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}

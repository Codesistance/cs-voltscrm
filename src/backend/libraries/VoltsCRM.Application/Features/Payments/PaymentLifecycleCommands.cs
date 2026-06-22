using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Payments;

public sealed record CompletePaymentCommand(Guid Id) : IRequest;
public sealed record FailPaymentCommand(Guid Id) : IRequest;
public sealed record ReversePaymentCommand(Guid Id) : IRequest;

public sealed class CompletePaymentHandler(IAppDbContext db) : IRequestHandler<CompletePaymentCommand>
{
    public async Task Handle(CompletePaymentCommand cmd, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Payment), cmd.Id);

        if (payment.Status != PaymentStatus.Pending)
            throw new ValidationException([new ValidationFailure("status", "Only pending payments can be completed.")]);

        payment.Complete();
        await PaymentCompletionHelper.ApplyCompletionEffectsAsync(db, payment, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class FailPaymentHandler(IAppDbContext db) : IRequestHandler<FailPaymentCommand>
{
    public async Task Handle(FailPaymentCommand cmd, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Payment), cmd.Id);

        if (payment.Status != PaymentStatus.Pending)
            throw new ValidationException([new ValidationFailure("status", "Only pending payments can be failed.")]);

        payment.Fail();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ReversePaymentHandler(IAppDbContext db) : IRequestHandler<ReversePaymentCommand>
{
    public async Task Handle(ReversePaymentCommand cmd, CancellationToken ct)
    {
        var payment = await db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == cmd.Id, ct)
            ?? throw new NotFoundException(nameof(Payment), cmd.Id);

        if (payment.Status != PaymentStatus.Completed)
            throw new ValidationException([new ValidationFailure("status", "Only completed payments can be reversed.")]);

        // Undo allocation effects before reversing
        await PaymentCompletionHelper.ReverseCompletionEffectsAsync(db, payment, ct);
        payment.Reverse();
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Helper for applying/reversing payment completion effects:
/// - Invoice.RecordPayment / UndoPayment
/// - Installment.MarkPaid / UndoPayment
/// - PaymentAccount.Credit / Debit (for unallocated overpayment)
/// </summary>
internal static class PaymentCompletionHelper
{
    public static async Task ApplyCompletionEffectsAsync(IAppDbContext db, Payment payment, CancellationToken ct)
    {
        if (payment.Status != PaymentStatus.Completed)
            payment.Complete();

        decimal allocated = 0m;

        // Process invoice allocations
        var invoiceAllocations = payment.Allocations.Where(a => a.InvoiceId.HasValue).ToList();
        if (invoiceAllocations.Count > 0)
        {
            var invoiceIds = invoiceAllocations.Select(a => a.InvoiceId!.Value).Distinct().ToList();
            var invoices = await db.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToListAsync(ct);

            foreach (var alloc in invoiceAllocations)
            {
                var invoice = invoices.First(i => i.Id == alloc.InvoiceId);
                invoice.RecordPayment(alloc.Amount);
                allocated += alloc.Amount;
            }
        }

        // Process installment allocations
        var installmentAllocations = payment.Allocations.Where(a => a.InstallmentId.HasValue).ToList();
        if (installmentAllocations.Count > 0)
        {
            var installmentIds = installmentAllocations.Select(a => a.InstallmentId!.Value).Distinct().ToList();
            var installments = await db.Installments.Where(i => installmentIds.Contains(i.Id)).ToListAsync(ct);

            foreach (var alloc in installmentAllocations)
            {
                var installment = installments.First(i => i.Id == alloc.InstallmentId);
                installment.MarkPaid(payment.PaymentDate);
                allocated += alloc.Amount;
            }
        }

        // Credit unallocated amount (overpayment) to customer's payment account
        var unallocated = payment.NetAmount - allocated;
        if (unallocated > 0)
        {
            var account = await GetOrCreatePaymentAccountAsync(db, payment.CustomerId, payment.Currency, ct);
            account.Credit(unallocated, payment.PaymentDate);
        }
    }

    public static async Task ReverseCompletionEffectsAsync(IAppDbContext db, Payment payment, CancellationToken ct)
    {
        decimal allocated = 0m;

        // Reverse invoice allocations
        var invoiceAllocations = payment.Allocations.Where(a => a.InvoiceId.HasValue).ToList();
        if (invoiceAllocations.Count > 0)
        {
            var invoiceIds = invoiceAllocations.Select(a => a.InvoiceId!.Value).Distinct().ToList();
            var invoices = await db.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToListAsync(ct);

            foreach (var alloc in invoiceAllocations)
            {
                var invoice = invoices.First(i => i.Id == alloc.InvoiceId);
                invoice.UndoPayment(alloc.Amount);
                allocated += alloc.Amount;
            }
        }

        // Reverse installment allocations
        var installmentAllocations = payment.Allocations.Where(a => a.InstallmentId.HasValue).ToList();
        if (installmentAllocations.Count > 0)
        {
            var installmentIds = installmentAllocations.Select(a => a.InstallmentId!.Value).Distinct().ToList();
            var installments = await db.Installments.Where(i => installmentIds.Contains(i.Id)).ToListAsync(ct);

            foreach (var alloc in installmentAllocations)
            {
                var installment = installments.First(i => i.Id == alloc.InstallmentId);
                installment.UndoPayment();
                allocated += alloc.Amount;
            }
        }

        // Debit the unallocated amount from the customer's payment account
        var unallocated = payment.NetAmount - allocated;
        if (unallocated > 0)
        {
            var account = await db.PaymentAccounts
                .FirstOrDefaultAsync(a => a.CustomerId == payment.CustomerId, ct);
            account?.Debit(unallocated);
        }
    }

    private static async Task<PaymentAccount> GetOrCreatePaymentAccountAsync(
        IAppDbContext db, Guid customerId, string currency, CancellationToken ct)
    {
        var account = await db.PaymentAccounts
            .FirstOrDefaultAsync(a => a.CustomerId == customerId, ct);

        if (account is null)
        {
            account = new PaymentAccount(customerId, currency);
            db.PaymentAccounts.Add(account);
        }

        return account;
    }
}

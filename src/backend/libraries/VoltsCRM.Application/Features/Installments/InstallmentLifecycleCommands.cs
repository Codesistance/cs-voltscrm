using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Exceptions;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Entities.Billing;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Installments;

/// <summary>Marks all pending installments past their due date as overdue.</summary>
public sealed record MarkInstallmentsOverdueCommand(DateTimeOffset? AsOf = null) : IRequest<MarkInstallmentsOverdueResult>;

/// <summary>Result of the mark-overdue operation.</summary>
public sealed record MarkInstallmentsOverdueResult(int MarkedCount);

/// <summary>Marks a single installment as paid.</summary>
public sealed record MarkInstallmentPaidCommand(Guid InstallmentId, DateTimeOffset? PaidAt = null) : IRequest;

public sealed class MarkInstallmentsOverdueHandler(IAppDbContext db)
    : IRequestHandler<MarkInstallmentsOverdueCommand, MarkInstallmentsOverdueResult>
{
    public async Task<MarkInstallmentsOverdueResult> Handle(MarkInstallmentsOverdueCommand cmd, CancellationToken ct)
    {
        var asOf = cmd.AsOf ?? DateTimeOffset.UtcNow;

        var pendingInstallments = await db.Installments
            .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .ToListAsync(ct);

        foreach (var installment in pendingInstallments)
            installment.MarkOverdue();

        await db.SaveChangesAsync(ct);

        return new MarkInstallmentsOverdueResult(pendingInstallments.Count);
    }
}

public sealed class MarkInstallmentPaidHandler(IAppDbContext db) : IRequestHandler<MarkInstallmentPaidCommand>
{
    public async Task Handle(MarkInstallmentPaidCommand cmd, CancellationToken ct)
    {
        var installment = await db.Installments.FirstOrDefaultAsync(i => i.Id == cmd.InstallmentId, ct)
            ?? throw new NotFoundException(nameof(Installment), cmd.InstallmentId);

        if (installment.Status == InstallmentStatus.Paid)
            throw new ValidationException([new ValidationFailure("status", "Installment is already paid.")]);

        installment.MarkPaid(cmd.PaidAt ?? DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
    }
}

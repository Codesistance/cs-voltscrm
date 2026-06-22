using MediatR;
using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Domain.Enums;

namespace VoltsCRM.Application.Features.Invoices;

/// <summary>Marks all pending invoices past their due date as overdue.</summary>
public sealed record MarkInvoicesOverdueCommand(DateTimeOffset? AsOf = null) : IRequest<MarkOverdueResult>;

/// <summary>Result of the mark-overdue operation.</summary>
public sealed record MarkOverdueResult(int MarkedCount);

public sealed class MarkInvoicesOverdueHandler(IAppDbContext db) : IRequestHandler<MarkInvoicesOverdueCommand, MarkOverdueResult>
{
    public async Task<MarkOverdueResult> Handle(MarkInvoicesOverdueCommand cmd, CancellationToken ct)
    {
        var asOf = cmd.AsOf ?? DateTimeOffset.UtcNow;

        var pendingInvoices = await db.Invoices
            .Where(i => i.Status == InvoiceStatus.Pending && i.DueDate < asOf)
            .ToListAsync(ct);

        foreach (var invoice in pendingInvoices)
            invoice.MarkOverdue();

        await db.SaveChangesAsync(ct);

        return new MarkOverdueResult(pendingInvoices.Count);
    }
}

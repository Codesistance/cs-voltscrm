using Microsoft.EntityFrameworkCore;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Models;
using VoltsCRM.Domain.Entities.Billing;

namespace VoltsCRM.Application.Features.Payments;

public static class PaymentMapping
{
    public static PaymentListItemDto ToListItem(this Payment p, string customerName) => new(
        p.Id, p.CustomerId, customerName,
        new MoneyDto(p.Amount, p.Currency), new MoneyDto(p.NetAmount, p.Currency),
        p.Method.ToString(), p.Channel.ToString(), p.Status.ToString(), p.PaymentDate, p.PlatformReference);

    public static async Task<PaymentDto> ToDetailDtoAsync(this Payment p, IAppDbContext db, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.Id == p.CustomerId)
            .Select(c => new { c.AccountNumber, c.PersonalInfo.FirstName, c.PersonalInfo.LastName })
            .FirstOrDefaultAsync(ct);

        var allocations = p.Allocations
            .Select(a => new PaymentAllocationDto(a.Id, a.InvoiceId, a.InstallmentId, a.Amount))
            .ToList();

        return new PaymentDto(
            p.Id, p.CustomerId,
            customer is null ? "—" : $"{customer.FirstName} {customer.LastName}",
            customer?.AccountNumber ?? "—",
            new MoneyDto(p.Amount, p.Currency), p.DiscountApplied, new MoneyDto(p.NetAmount, p.Currency),
            p.Method.ToString(), p.Channel.ToString(), p.Status.ToString(),
            p.PlatformProvider, p.PlatformReference, p.CollectedByAgentId,
            p.PaymentDate, allocations, p.CreatedAt, p.UpdatedAt);
    }
}
